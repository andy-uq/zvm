﻿module Story

  open Types
  open Utility

  type t = { dynamic_memory : Immutable_bytes.t; static_memory : byte[]; }

  let make dynamic_memory static_memory =
    {
        dynamic_memory = Immutable_bytes.make dynamic_memory;
        static_memory = static_memory;
    }

  let read_byte story address =
    let dynamic_size = Immutable_bytes.size story.dynamic_memory
    if is_in_range address dynamic_size then
      Immutable_bytes.read_byte story.dynamic_memory address
    else
      let static_addr = dec_byte_addr_by address dynamic_size
      dereference_array static_addr story.static_memory 

  let read_word story address =
   let high = read_byte story (address_of_high_byte address)
   let low = read_byte story (address_of_low_byte address)
   256 * high + low

  let write_byte story address value =
    let dynamic_memory = Immutable_bytes.write_byte story.dynamic_memory address value
    { story with dynamic_memory = dynamic_memory }

  let write_word story address value =
    let high = (value <<< 8) &&& 0xFF
    let low = value &&& 0xFF
    let story = write_byte story (address_of_high_byte address) high
    write_byte story (address_of_low_byte address) low

  let header_size = 64
  let static_memory_base_offset = Word_address 14
  let version_offset = Byte_address 0
  let version story =
    match read_byte story version_offset with
    | 1 -> V1
    | 2 -> V2
    | 3 -> V3
    | 4 -> V4
    | 5 -> V5
    | 6 -> V6
    | 7 -> V7
    | 8 -> V8
    | _ -> failwith "unknown version"
  
  let v5_or_lower v =
    match v with
    | V1  | V2  | V3  | V4 | V5 -> true
    | V6  | V7  | V8 -> false

  let v6_or_higher v =
    not (v5_or_lower v)

  let v4_or_lower v =
    match v with
    | V1  | V2  | V3  | V4 -> true
    | V5  | V6  | V7  | V8 -> false

  let v5_or_higher v =
    not (v4_or_lower v)

  let v3_or_lower v =
    match v with
    | V1  | V2  | V3 -> true
    | V4  | V5  | V6  | V7  | V8 -> false

  let v4_or_higher v =
    not (v3_or_lower v)
  let dictionary_base story =
    let dictionary_base_offset = Word_address 8
    Dictionary_base (read_word story dictionary_base_offset)

  let abbreviations_table_base story =
    let abbreviations_table_base_offset = Word_address 24
    Abbreviation_table_base (read_word story abbreviations_table_base_offset)

  let object_table_base story =
    let object_table_base_offset = Word_address 10
    Object_base (read_word story object_table_base_offset)

  let routine_offset story =
    let routine_offset_offset = Word_address 40
    8 * (read_word story routine_offset_offset)

  let string_offset story =
    let string_offset_offset = Word_address 42
    8 * (read_word story string_offset_offset)

  let global_variables_table_base story =
    let global_variables_table_base_offset = Word_address 12
    Global_table_base (read_word story global_variables_table_base_offset)

  let load filename =
    let file = get_file filename
    let len = Array.length file
    if len < header_size then
      failwith (Printf.sprintf "%s is not a valid story file" filename)
    else
      let high = dereference_array (address_of_high_byte static_memory_base_offset) file
      let low = dereference_array (address_of_low_byte static_memory_base_offset) file
      let dynamic_length = high * 256 + low
      if dynamic_length > len then
        failwith (Printf.sprintf "%s is not a valid story file because could not decode header" filename)
      else 
        let dynamic_memory = Array.take dynamic_length file
        let static_memory = Array.skip dynamic_length file
        make dynamic_memory static_memory

  let decode_routine_packed_address story (Packed_routine packed) =
    match version story with
    | V1
    | V2
    | V3 -> Routine (packed * 2)
    | V4
    | V5 -> Routine (packed * 4)
    | V6
    | V7 -> Routine (packed * 4 + (routine_offset story))
    | V8 -> Routine (packed * 8)

  let decode_string_packed_address story (Packed_zstring packed) =
    match version story with
    | V1
    | V2
    | V3 -> Zstring (packed * 2)
    | V4
    | V5 -> Zstring (packed * 4)
    | V6
    | V7 -> Zstring (packed * 4 + (string_offset story))
    | V8 -> Zstring (packed * 8)