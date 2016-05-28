module Immutable_bytes
  open Types
  open Utility

  type t =  { original_bytes : string; edits : IntMap<char> }

  let make bytes =
    { original_bytes = bytes; edits = Map.empty }

  let size bytes =
    String.length bytes.original_bytes

  let read_byte bytes address =
    if is_out_of_range address (size bytes) then
      failwith "address is out of range"
    else
      let (Byte_address addr) = address in
      let c =
        match (Map.tryFind addr bytes.edits) with
        | None -> bytes.original_bytes.[addr]
        | Some a -> a in
      int c

  let write_byte bytes address value =
    if is_out_of_range address (size bytes) then
      failwith "address is out of range"
    else
      let (Byte_address addr) = address in
      let b = char value in
      { bytes with edits = Map.add addr b bytes.edits }