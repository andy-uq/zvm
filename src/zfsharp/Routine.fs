module Routine
  open Types
  open Utility

  type t =
    {
      locals : IntMap<int>;
      count : int;
      arguments_supplied : int;
    }

  let empty =
    { locals = Map.empty; count = 0; arguments_supplied = 0 }

  let locals_count story (Routine routine_address) =
    Story.read_byte story (Byte_address routine_address)

  let first_instruction story (Routine routine_address) =
    if Story.v4_or_lower (Story.version story) then
      let count = locals_count story (Routine routine_address) in
      Instruction (routine_address + 1 + count * word_size)
    else
      Instruction (routine_address + 1)

  let local_default_value story (Routine routine_address) n =
    if Story.v4_or_lower (Story.version story) then
      let addr = Word_address(routine_address + 1) in
      Story.read_word story (inc_word_addr_by addr (n - 1))
    else
      0

  let read_local local_store (Local local) =
    Map.find local local_store.locals

  let write_local local_store (Local local) value =
    let value = unsigned_word value
    let locals = Map.add local value local_store.locals
    { local_store with locals = locals }