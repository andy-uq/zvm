module Globals
  open Types
  open Utility

  let first_global = 16
  let last_global = 255

  let global_addr story (Global _global) = 
    if _global < first_global || _global > last_global then
        failwith "global variable index out of range"
    else
      let (Global_table_base _base) = Story.global_variables_table_base story in
      let _base = Word_address _base in
      let offset = _global - first_global in
      inc_word_addr_by _base offset

  let read story _global =
      Story.read_word story (global_addr story _global)

  let write story _global value =
      Story.write_word story (global_addr story _global) value
    
  let display story =
    let to_string g =
      Printf.sprintf "%02x %04x\n" (g - first_global) (read story (Global g)) in
    accumulate_strings_loop to_string first_global (last_global + 1)