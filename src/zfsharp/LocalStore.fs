module Local_store
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

  let read_local local_store (Local local) =
    Map.find local local_store.locals

  let write_local local_store (Local local) value =
    let value = unsigned_word value
    let locals = Map.add local value local_store.locals
    { local_store with locals = locals }

  let add local_store (Local n) default_value =
    let locals = Map.add n default_value local_store.locals
    let count = max local_store.count n
    { local_store with locals = locals; count = count }

  let create_default_locals story routine_address =
    let count = Routine.locals_count story routine_address
    let rec aux acc i =
      if i > count then
        acc
      else
        let default_value = Routine.local_default_value story routine_address i
        let new_store = add acc (Local i) default_value
        aux new_store (i + 1)
    aux empty 1

  let write_arguments local_store arguments =
    let rec aux acc args i =
      match args with
      | [] -> acc
      | arg :: tail ->
        if i > acc.count then
          acc
        else
          let new_store = write_local acc (Local i) arg
          aux new_store tail (i + 1)
    aux local_store arguments 1

  let display local_store =
    let to_string local value =
      Printf.sprintf "local%01x=%04x " (local - 1) value
    let folder acc local value =
      acc + (to_string local value)
    let locals = local_store.locals
    Map.fold folder "" locals