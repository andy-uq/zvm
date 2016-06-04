module Interpreter
  open Types
  open Utility

  type t =
    {
      story : Story.t;
      program_counter : instruction_address;
      frames : Frameset.t;
    }

  let make story =
    {
      story = story;
      program_counter = Story.initial_program_counter story;
      frames = Frameset.make Frame.empty;
    }

  let current_frame interpreter =
    Frameset.current_frame interpreter.frames

  let peek_stack interpreter =
    Frameset.peek_stack interpreter.frames

  let pop_stack interpreter =
    { interpreter with frames = Frameset.pop_stack interpreter.frames }

  let push_stack interpreter value =
    { interpreter with frames = Frameset.push_stack interpreter.frames value }

  let read_local interpreter local =
    Frameset.read_local interpreter.frames local

  let write_local interpreter local value =
    { interpreter with frames = Frameset.write_local interpreter.frames local value }

  let read_global interpreter _global =
    Globals.read interpreter.story _global

  let write_global interpreter _global value =
    { interpreter with story = Globals.write interpreter.story _global value }

  let read_variable interpreter variable =
    match variable with
    | Stack -> (peek_stack interpreter, pop_stack interpreter)
    | Local_variable local -> (read_local interpreter local, interpreter)
    | Global_variable _global -> (read_global interpreter _global, interpreter)

  let write_variable interpreter variable value =
    match variable with
    | Stack -> push_stack interpreter value
    | Local_variable local -> write_local interpreter local value
    | Global_variable _global -> write_global interpreter _global value

  let read_variable_in_place interpreter variable =
    match variable with
    | Stack -> peek_stack interpreter
    | Local_variable local -> read_local interpreter local
    | Global_variable _global -> read_global interpreter _global

  let write_variable_in_place interpreter variable value =
    match variable with
    | Stack -> push_stack (pop_stack interpreter) value
    | Local_variable local -> write_local interpreter local value
    | Global_variable _global -> write_global interpreter _global value

  let read_operand interpreter operand =
    match operand with
    | Large large -> (large, interpreter)
    | Small small -> (small, interpreter)
    | Variable v -> read_variable interpreter v

  let operands_to_arguments interpreter operands =
    let rec aux (args, interp) ops =
      match ops with
      | [] -> (args, interp)
      | h :: t ->
        let (argument, new_interpreter) = read_operand interp h
        aux ((argument :: args), new_interpreter) t
    let (args_rev, final_interpreter) = aux ([], interpreter) operands
    ((List.rev args_rev), final_interpreter)

  let print interpreter (text : string) =
    System.Console.Write text;
    interpreter

  let interpret_store interpreter store result =
    match store with
    | None -> interpreter
    | Some variable -> write_variable interpreter variable result

  let add_frame interpreter frame =
    { interpreter with frames = Frameset.add_frame interpreter.frames frame }

  let remove_frame interpreter =
    { interpreter with frames = Frameset.remove_frame interpreter.frames }

  let set_program_counter interpreter program_counter =
    { interpreter with program_counter = program_counter }

  let interpret_return interpreter value =
    let frame = current_frame interpreter
    let next_pc = Frame.resume_at frame
    let store = Frame.store frame
    let pop_frame_interpreter = remove_frame interpreter
    let result_interpreter = set_program_counter pop_frame_interpreter next_pc
    interpret_store result_interpreter store value

  let interpret_branch interpreter instruction result =
    let result = not (result = 0)
    let following = Instruction.following instruction
    match Instruction.branch instruction with
    | None -> set_program_counter interpreter following
    | Some (sense, Return_false) ->
      if result = sense then interpret_return interpreter 0
      else set_program_counter interpreter following
    | Some (sense, Return_true) ->
      if result = sense then interpret_return interpreter 1
      else set_program_counter interpreter following
    | Some (sense, Branch_address branch_target) ->
      if result = sense then set_program_counter interpreter branch_target
      else set_program_counter interpreter following
  
  let interpret_instruction interpreter instruction handler = 
    let (result, handler_interpreter) = handler interpreter
    let store = Instruction.store instruction
    let store_interpreter = interpret_store handler_interpreter store result
    interpret_branch store_interpreter instruction result

  let interpret_value_instruction interpreter instruction handler =
    let result = handler interpreter
    let store = Instruction.store instruction
    let store_interpreter = interpret_store interpreter store result
    interpret_branch store_interpreter instruction result

  let interpret_effect_instruction interpreter instruction handler =
    let handler_interpreter = handler interpreter
    let result = 0
    let store = Instruction.store instruction
    let store_interpreter = interpret_store handler_interpreter store result
    interpret_branch store_interpreter instruction result

  (* This routine handles all call instructions:
     2OP:25  call_2s  routine arg -> (result)
     2OP:26  call_2n  routine arg
     1OP:136 call_1s  routine -> (result)
     1OP:143 call_1n  routine
     VAR:224 call_vs  routine up-to-3-arguments -> (result)
     VAR:236 call_vs2 routine up-to-7-arguments -> (result)
     VAR:249 call_vn  routine up-to-3-arguments
     VAR:250 call_vn2 routine up-to-7-arguments
     The "s" versions store the result; the "n" versions discard it. *)
  let handle_call routine_address arguments interpreter instruction =
    if routine_address = 0 then
      let result = 0
      let store = Instruction.store instruction
      let store_interpreter = interpret_store interpreter store result
      let addr = Instruction.following instruction
      set_program_counter store_interpreter addr
    else
      let routine_address = Packed_routine routine_address
      let routine_address = Story.decode_routine_packed_address interpreter.story routine_address
      let resume_at = Instruction.following instruction
      let store = Instruction.store instruction
      let frame = Frame.make interpreter.story arguments routine_address resume_at store
      let pc = Routine.first_instruction interpreter.story routine_address
      set_program_counter (add_frame interpreter frame) pc

  let handle_load variable interpreter =
    let variable = Instruction.decode_variable variable
    read_variable_in_place interpreter variable

  let handle_store variable value interpreter =
    let variable = Instruction.decode_variable variable
    write_variable_in_place interpreter variable value

  let handle_pull1 x interpreter =
    if (Story.version interpreter.story) = V6 then
      failwith "TODO: user stack pull not yet implemented"
    else
      let variable = Instruction.decode_variable x
      let value = peek_stack interpreter
      let popped_interpreter = pop_stack interpreter
      let store_interpreter = write_variable_in_place popped_interpreter variable value
      (0, store_interpreter)

  let handle_pull0 interpreter =
    (* In version 6 if the operand is omitted then we simply pop the stack
    and store the result normally. *)
    let result = peek_stack interpreter
    let popped_interpreter = pop_stack interpreter
    (result, popped_interpreter)

  let handle_inc variable interpreter =
    let variable = Instruction.decode_variable variable
    let original = read_variable_in_place interpreter variable
    let incremented = original + 1
    write_variable_in_place interpreter variable incremented

  let handle_dec variable interpreter =
    let variable = Instruction.decode_variable variable
    let original = read_variable_in_place interpreter variable
    let decremented = original - 1
    write_variable_in_place interpreter variable decremented

  let handle_inc_chk variable value interpreter =
    let variable = Instruction.decode_variable variable
    let value = signed_word value
    let original = read_variable_in_place interpreter variable
    let original = signed_word original
    let incremented = signed_word (original + 1)
    let write_interpreter = write_variable_in_place interpreter variable incremented
    let result = if incremented > value then 1 else 0
    (result, write_interpreter)

  let handle_dec_chk variable value interpreter =
    let variable = Instruction.decode_variable variable
    let value = signed_word value
    let original = read_variable_in_place interpreter variable
    let original = signed_word original
    let decremented = signed_word (original + 1)
    let write_interpreter = write_variable_in_place interpreter variable decremented
    let result = if decremented < value then 1 else 0
    (result, write_interpreter)
  
  (* Spec: 2OP:1 je a b ?(label)
     Jump if a = b. *)
  let handle_je2 a b interpreter =
    if a = b then 1 else 0

  let handle_je3 a b c interpreter =
    if a = b || a = c then 1 else 0

  let handle_je4 a b c d interpreter =
    if a = b || a = c || a = d then 1 else 0

  (* Spec: 2OP:2 jl a b ?(label)
     Jump if a < b  using a signed 16-bit comparison. *)
  let handle_jl a b interpreter =
    let a = signed_word a
    let b = signed_word b
    if a < b then 1 else 0

  (* Spec: 2OP:3 jg a b ?(label)
     Jump if a > b  using a signed 16-bit comparison. *)
  let handle_jg a b interpreter =
    let a = signed_word a
    let b = signed_word b
    if a > b then 1 else 0

  (* Spec: 2OP:20 add a b -> (result)
     Signed 16-bit addition. *)
  let handle_add a b interpreter =
    a + b

  (* Spec: 2OP:21 add a b -> (result)
     Signed 16-bit subtraction. *)
  let handle_sub a b interpreter =
    a - b

  (* Spec: 2OP:22 add a b -> (result)
     Signed 16-bit multiplication. *)
  let handle_mul a b interpreter =
    a * b

  (* Spec: 2OP:23 add a b -> (result)
     Signed 16-bit division. *)
  let handle_div a b interpreter =
    let a = signed_word a
    let b = signed_word b
    a / b

  (* Spec: 2OP:24 add a b -> (result)
     Signed 16-bit modulo. *)
  let handle_mod a b interpreter =
    let a = signed_word a
    let b = signed_word b
    a % b

  (* Spec: 1OP:128 jz a ?(label)
     Jump if a = 0. *)
  let handle_jz a interpreter =
    if a = 0 then 1 else 0

  let handle_loadw arr idx interpreter =
    let arr = Word_address arr
    let addr = inc_word_addr_by arr idx
    Story.read_word interpreter.story addr

  let handle_loadb arr idx interpreter =
    let arr = Byte_address arr
    let addr = inc_byte_addr_by arr idx
    Story.read_byte interpreter.story addr

  let handle_storew arr idx value interpreter =
    let arr = Word_address arr
    let addr = inc_word_addr_by arr idx
    { interpreter with story = Story.write_word interpreter.story addr value }

  let handle_storeb arr idx value interpreter =
    let arr = Byte_address arr
    let addr = inc_byte_addr_by arr idx
    { interpreter with story = Story.write_byte interpreter.story addr value }

  let handle_ret result interpreter =
    interpret_return interpreter result

  let handle_jump offset interpreter instruction =
    let offset = signed_word offset
    let target = Instruction.jump_address instruction offset
    set_program_counter interpreter target

  let handle_jin obj1 obj2 interpreter =
    let obj1 = Object obj1
    let obj2 = Object obj2
    let parent = Object.parent interpreter.story obj1
    if parent = obj2 then 1 else 0

  let handle_get_sibling obj interpreter =
    let obj = Object obj
    let (Object sibling) = Object.sibling interpreter.story obj
    sibling

  let handle_get_child obj interpreter =
    let obj = Object obj
    let (Object child) = Object.child interpreter.story obj
    child

  let handle_get_parent obj interpreter =
    let obj = Object obj
    let (Object parent) = Object.parent interpreter.story obj
    parent

  let handle_insert_obj obj destination interpreter =
    let obj = Object obj
    let destination = Object destination
    { interpreter with story = Object.insert interpreter.story obj destination}

  let handle_remove_obj obj interpreter =
    let obj = Object obj
    { interpreter with story = Object.remove interpreter.story obj}

  let handle_print_addr address interpreter =
    let address = Zstring address
    let text = Zstring.read interpreter.story address
    print interpreter text

  let handle_print_paddr packed_address interpreter =
    let packed_address = Packed_zstring packed_address
    let address = Story.decode_string_packed_address interpreter.story packed_address
    let text = Zstring.read interpreter.story address
    print interpreter text

  let handle_print interpreter instruction =
    let printed_interpreter = 
      match Instruction.text instruction with
      | Some text -> print interpreter text
      | None -> interpreter
    interpret_branch printed_interpreter instruction 0

  let handle_print_obj obj interpreter =
    let obj = Object obj
    let text = Object.name interpreter.story obj
    print interpreter text

  let handle_print_ret interpreter instruction =
    let printed_interpreter =
      match Instruction.text instruction with
      | Some text -> print interpreter (text + "\n")
      | None -> interpreter 
    interpret_return printed_interpreter 1

  let handle_new_line interpreter =
    print interpreter "\n"

  let handle_print_num value interpreter =
    let value = signed_word value
    print interpreter (string value)

  let handle_print_char (code : int) interpreter =
    let text = string_of_char (char code)
    print interpreter text

  let handle_test bitmap flags interpreter =
    if (bitmap &&& flags) = flags then 1 else 0

  let handle_test_attr obj attr interpreter =
    let obj = Object obj in
    let attr = Attribute attr in
    if Object.attribute interpreter.story obj attr then 1 else 0  

  let handle_set_attr obj attr interpreter =
    let obj = Object obj in
    let attr = Attribute attr in
    { interpreter with story = Object.set_attribute interpreter.story obj attr }

  let handle_clear_attr obj attr interpreter =
    let obj = Object obj in
    let attr = Attribute attr in
    { interpreter with story = Object.clear_attribute interpreter.story obj attr }

  let handle_or a b interpreter =
    a ||| b

  let handle_and a b interpreter =
    a &&& b

  let handle_rtrue interpreter instruction =
    interpret_return interpreter 1

  let handle_rfalse interpreter instruction =
    interpret_return interpreter 0

  let handle_ret_popped interpreter instruction =
    let result = peek_stack interpreter
    let popped_interpreter = pop_stack interpreter
    interpret_return popped_interpreter result

  let handle_pop interpreter =
    pop_stack interpreter

  let handle_get_next_prop obj prop interpreter =
    let obj = Object obj in
    let prop = Property prop in
    let (Property next) = Object.next_property interpreter.story obj prop in
    next  
  
  let handle_get_prop_addr obj prop interpreter =
    let obj = Object obj in
    let prop = Property prop in
    let (Property_data addr) = Object.property_address interpreter.story obj prop in
    addr

  let handle_get_prop obj prop interpreter =
    let obj = Object obj in
    let prop = Property prop in
    Object.property interpreter.story obj prop
 
  let handle_get_prop_len property_address interpreter =
    let property_address = Property_data property_address in
    Object.property_length_from_address interpreter.story property_address

  let handle_putprop obj prop value interpreter =
    let obj = Object obj in
    let prop = Property prop in
    { interpreter with story = Object.write_property interpreter.story obj prop value }
  
  let handle_catch interpreter =
    failwith "TODO: catch instruction not yet implemented"

  let step_instruction interpreter =
    let instruction = Instruction.decode interpreter.story interpreter.program_counter
    let operands = Instruction.operands instruction
    let (arguments, interpreter) = operands_to_arguments interpreter operands
    let interpret_instruction = interpret_instruction interpreter instruction
    let value = interpret_value_instruction interpreter instruction
    let effect = interpret_effect_instruction interpreter instruction
    let opcode = Instruction.opcode instruction
    match (opcode, arguments) with
    | (OP2_1, [a; b]) -> value (handle_je2 a b)
    | (OP2_1, [a; b; c]) -> value (handle_je3 a b c)
    | (OP2_1, [a; b; c; d]) -> value (handle_je4 a b c d)
    | (OP2_2, [a; b]) -> value (handle_jl a b)
    | (OP2_3, [a; b]) -> value (handle_jg a b)
    | (OP2_4, [variable; value]) -> interpret_instruction (handle_dec_chk variable value)
    | (OP2_5, [variable; value]) -> interpret_instruction (handle_inc_chk variable value)
    | (OP2_6, [obj1; obj2]) -> value (handle_jin obj1 obj2)
    | (OP2_7, [bitmap; flags]) -> value (handle_test bitmap flags)
    | (OP2_8, [a; b]) -> value (handle_or a b)
    | (OP2_9, [a; b]) -> value (handle_and a b)
    | (OP2_10, [obj; attr]) -> value (handle_test_attr obj attr)
    | (OP2_11, [obj; attr]) -> effect (handle_set_attr obj attr)
    | (OP2_12, [obj; attr]) -> effect (handle_clear_attr obj attr)
    | (OP2_13, [variable; value]) -> effect (handle_store variable value)
    | (OP2_14, [obj; destination]) -> effect (handle_insert_obj obj destination)
    | (OP2_15, [arr; idx]) -> value (handle_loadw arr idx)
    | (OP2_16, [arr; idx]) -> value (handle_loadb arr idx)
    | (OP2_17, [obj; prop]) -> value (handle_get_prop obj prop)
    | (OP2_18, [obj; prop]) -> value (handle_get_prop_addr obj prop)
    | (OP2_19, [obj; prop]) -> value (handle_get_next_prop obj prop)
    | (OP2_20, [a; b]) -> value (handle_add a b)
    | (OP2_21, [a; b]) -> value (handle_sub a b)
    | (OP2_22, [a; b]) -> value (handle_mul a b)
    | (OP2_23, [a; b]) -> value (handle_div a b)
    | (OP2_24, [a; b]) -> value (handle_mod a b)
    | (OP1_128, [a]) -> value (handle_jz a)
    | (OP1_129, [obj]) -> value (handle_get_sibling obj)
    | (OP1_130, [obj]) -> value (handle_get_child obj)
    | (OP1_131, [obj]) -> value (handle_get_parent obj)
    | (OP1_132, [property_address]) -> value (handle_get_prop_len property_address)
    | (OP1_133, [variable]) -> effect (handle_inc variable)
    | (OP1_134, [variable]) -> effect (handle_dec variable)
    | (OP1_135, [address]) -> effect (handle_print_addr address)
    | (OP1_137, [obj]) -> effect (handle_remove_obj obj)
    | (OP1_138, [obj]) -> effect (handle_print_obj obj)
    | (OP1_139, [result]) -> handle_ret result interpreter 
    | (OP1_140, [offset]) -> handle_jump offset interpreter instruction
    | (OP1_141, [paddr]) -> effect (handle_print_paddr paddr)
    | (OP1_142, [variable]) -> value (handle_load variable)
    | (OP0_176, []) -> handle_rtrue interpreter instruction
    | (OP0_177, []) -> handle_rfalse interpreter instruction
    | (OP0_178, []) -> handle_print interpreter instruction
    | (OP0_179, []) -> handle_print_ret interpreter instruction
    | (OP0_184, []) -> handle_ret_popped interpreter instruction
    | (OP0_185, []) ->
      if Story.v4_or_lower (Story.version interpreter.story) then effect handle_pop
      else value handle_catch
    | (OP0_187, []) -> effect handle_new_line
    | (VAR_224, routine :: args) -> handle_call routine args interpreter instruction
    | (VAR_225, [arr; ind; value]) -> effect (handle_storew arr ind value)
    | (VAR_226, [arr; ind; value]) -> effect (handle_storeb arr ind value)
    | (VAR_227, [obj; prop; value]) -> effect (handle_putprop obj prop value)
    | (VAR_229, [code]) -> effect (handle_print_char code)
    | (VAR_230, [number]) -> effect (handle_print_num number)
    | (VAR_233, []) -> interpret_instruction handle_pull0
    | (VAR_233, [x]) -> interpret_instruction (handle_pull1 x)
    | _ -> failwith (Printf.sprintf "TODO: %s " (Instruction.display instruction interpreter.story))

  let display_current_instruction interpreter =
    let address = interpreter.program_counter
    let instruction = Instruction.decode interpreter.story address
    Instruction.display instruction interpreter.story

  let display interpreter =
    let frames = Frameset.display interpreter.frames
    let instr = display_current_instruction interpreter
    Printf.sprintf "\n---\n%s\n%s" frames instr
