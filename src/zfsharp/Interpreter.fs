module Interpreter
  open Types

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

  let handle_ret result interpreter =
      interpret_return interpreter result

  let step_instruction interpreter =
    let instruction = Instruction.decode interpreter.story interpreter.program_counter in
    let operands = Instruction.operands instruction in
    let (arguments, interpreter) = operands_to_arguments interpreter operands in
    let opcode = Instruction.opcode instruction in
    match (opcode, arguments) with
    | (OP1_139, [result]) -> handle_ret result interpreter
    | (VAR_224, routine :: args) -> handle_call routine args interpreter instruction
    | _ -> failwith (Printf.sprintf "TODO: %s " (Instruction.display instruction interpreter.story))

  let display_current_instruction interpreter =
    let address = interpreter.program_counter
    let instruction = Instruction.decode interpreter.story address
    Instruction.display instruction interpreter.story

  let display interpreter =
    let frames = Frameset.display interpreter.frames
    let instr = display_current_instruction interpreter
    Printf.sprintf "\n---\n%s\n%s\n" frames instr