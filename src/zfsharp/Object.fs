module Object

  open Types
  open Utility

  (* The object table is laid out as follows:

  * The base of the object table is in the header.
  * The object table begins with a block of 31 or 63 default property values.
  * Following the default property values is the object tree.
  * Each entry in the tree is of the same size, and is laid out as follows:
    * 32 or 48 bits of attribute flags
    * the parent, sibling and child object numbers
    * the address of an additional table of variable-sized properties.
  * object numbers are one-based, so zero is used as the invalid object.
  *)

  let invalid_object = Object 0
  let invalid_property = Property 0
  let invalid_data = Property_data 0

  let default_property_table_size story =
    if Story.v3_or_lower (Story.version story) then 31 else 63

  let default_property_table_entry_size = 2

  let tree_base story =
    let (Object_base _base) = Story.object_table_base story
    let table_size = default_property_table_size story
    Object_tree_base (_base + default_property_table_entry_size * table_size)

  let entry_size story =
    if Story.v3_or_lower (Story.version story) then 9 else 14

  let address story (Object obj) =
    let (Object_tree_base tree_base) = tree_base story
    let entry_size = entry_size story
    Object_address (tree_base + (obj - 1) * entry_size)

  let parent story obj =
    let (Object_address addr) = address story obj
    if Story.v3_or_lower (Story.version story) then
      Object (Story.read_byte story (Byte_address (addr + 4)))
    else
      Object (Story.read_word story (Word_address (addr + 6)))

  let set_parent story obj (Object new_parent) =
    let (Object_address addr) = address story obj
    if Story.v3_or_lower (Story.version story) then
      Story.write_byte story (Byte_address (addr + 4)) new_parent
    else
      Story.write_word story (Word_address (addr + 6)) new_parent

  let sibling story obj =
    let (Object_address addr) = address story obj
    if Story.v3_or_lower (Story.version story) then
      Object (Story.read_byte story (Byte_address (addr + 5)))
    else
      Object (Story.read_word story (Word_address (addr + 8)))

  let set_sibling story obj (Object new_sibling) =
    let (Object_address addr) = address story obj
    if Story.v3_or_lower (Story.version story) then
      Story.write_byte story (Byte_address (addr + 5)) new_sibling
    else
      Story.write_word story (Word_address (addr + 8)) new_sibling

  let child story obj =
    let (Object_address addr) = address story obj
    if Story.v3_or_lower (Story.version story) then
      Object (Story.read_byte story (Byte_address(addr + 6)))
    else
      Object (Story.read_word story (Word_address(addr + 10)))

  let set_child story obj (Object new_child) =
    let (Object_address addr) = address story obj
    if Story.v3_or_lower (Story.version story) then
      Story.write_byte story (Byte_address (addr + 6)) new_child
    else
      Story.write_word story (Word_address (addr + 10)) new_child

  let find_previous_sibling story obj =
    let rec aux current =
      let next_sibling = sibling story current
      if next_sibling = obj then current
      else aux next_sibling
    let parent = parent story obj
    let first_child = child story parent
    aux first_child

  let remove story obj =
    let original_parent = parent story obj
    if original_parent = invalid_object then
      story (* Already detatched *)
    else
      let edit1 = 
        let sibling = sibling story obj
        if obj = child story original_parent then
          set_child story original_parent sibling
        else
          let prev_sibling = find_previous_sibling story obj
          set_sibling story prev_sibling sibling
      set_parent edit1 obj invalid_object

  let attribute_count story =
    if Story.v3_or_lower (Story.version story) then 32 else 48

  let attribute_address story obj (Attribute attribute) =
    if attribute < 0 || attribute >= (attribute_count story) then
      failwith "bad attribute"
    else
      let offset = attribute / 8
      let (Object_address obj_addr) = address story obj
      let bit = Bit_number (7 - (attribute % 8))
      Attribute_address ((Byte_address (obj_addr + offset)), bit)

  let attribute story obj attribute =
    let (Attribute_address (address, bit)) = attribute_address story obj attribute
    Story.read_bit story address bit

  let set_attribute story obj attribute =
    let (Attribute_address (address, bit)) = attribute_address story obj attribute
    Story.write_set_bit story address bit

  let clear_attribute story obj attribute =
    let (Attribute_address (address, bit)) = attribute_address story obj attribute
    Story.write_clear_bit story address bit

  let insert story new_child new_parent =
    let edit1 = remove story new_child
    let edit2 = set_parent edit1 new_child new_parent
    let edit3 = set_sibling edit2 new_child (child edit2 new_parent)
    set_child edit3 new_parent new_child

  (* The last two bytes in an object description are a pointer to a
  block that contains additional properties. *)
  let property_header_address story obj =
    let object_property_offset =
      if Story.v3_or_lower (Story.version story) then 7 else 12
    let (Object_address addr) = address story obj
    Property_header (Story.read_word story (Word_address (addr + object_property_offset)))

  let default_property_table_base story =
    let (Object_base _base) = Story.object_table_base story
    Property_defaults_table _base

  let default_property_value story (Property n) =
    if n < 1 || n > (default_property_table_size story) then
      failwith "invalid index into default property table"
    else
      let (Property_defaults_table _base) = default_property_table_base story
      let addr = Word_address ((_base + (n - 1) * default_property_table_entry_size))
      Story.read_word story addr

  let decode_property_data story (Property_address address) = 
    let b = Story.read_byte story (Byte_address address)
    if b = 0 then
      (0, 0, invalid_property)
    else if Story.v3_or_lower (Story.version story) then
      (1, (fetch_bits bit7 size3 b) + 1, Property (fetch_bits bit4 size5 b))
    else
      let prop = Property (fetch_bits bit5 size6 b)
      if fetch_bit bit7 b then
        let b2 = Story.read_byte story (Byte_address (address + 1))
        let len = fetch_bits bit5 size6 b2
        (2, (if len = 0 then 64 else len), prop)
      else
        (1, (if fetch_bit bit6 b then 2 else 1), prop)

  let property_addresses story obj =
    let rec aux acc address =
      let (Property_address addr) = address
      let b = Story.read_byte story (Byte_address addr)
      if b = 0 then
        acc
      else
        let (header_length, data_length, prop) =
          decode_property_data story address
        let this_property =
          (prop, data_length, Property_data (addr + header_length))
        let next_addr = Property_address (addr + header_length + data_length)
        aux (this_property :: acc) next_addr
    let (Property_header header) = property_header_address story obj
    let property_name_address = header
    let property_name_word_length = Story.read_byte story (Byte_address property_name_address)
    let first_property_address =
      Property_address (property_name_address + 1 + property_name_word_length * 2)
    aux [] first_property_address  

  let property_address story obj prop =
    let rec aux addresses =
      match addresses with
      | [] -> invalid_data
      | (number, _, address) :: tail ->
        if number = prop then address
        else aux tail
    aux (property_addresses story obj)

  let property_length_from_address story (Property_data address) =
    if address = 0 then
      0
    else
      let b = Story.read_byte story (Byte_address (address - 1))
      if Story.v3_or_lower (Story.version story) then
        1 + (fetch_bits bit7 size3 b)
      else
        if fetch_bit bit7 b then
          let len = fetch_bits bit5 size6 b
          if len = 0 then 64 else len
        else
          if fetch_bit bit6 b then 2 else 1

  let property story obj prop =
    let rec aux addresses =
      match addresses with
      | [] -> default_property_value story prop
      | (number, length, (Property_data address)) :: tail ->
        if number = prop then (
          if length = 1 then
            Story.read_byte story (Byte_address address)
          else if length = 2 then
            Story.read_word story (Word_address address)
          else
            let (Object n) = obj
            let (Property p) = prop
            failwith (Printf.sprintf "object %d property %d length %d bad property length" n p length))
        else
          aux tail
    aux (property_addresses story obj)

  let write_property story obj prop value =
    let rec aux addresses =
      match addresses with
      | [] -> (invalid_data, 0)
      | (number, length, address) :: tail ->
        if number = prop then (address, length)
        else aux tail
    let (address, length) = aux (property_addresses story obj)
    if address = invalid_data then failwith "invalid property";
    let (Property_data address) = address
    match length with
    | 1 -> Story.write_byte story (Byte_address address) value
    | 2 -> Story.write_word story (Word_address address) value
    | _ -> failwith "property cannot be set"

  let next_property story obj (Property prop) =
    let rec aux addrs =
      match addrs with
      | [] -> invalid_property
      | (Property number, _, _) :: tail ->
        if number > prop then Property number
        else aux tail
    aux (property_addresses story obj)

  (* Oddly enough, the Z machine does not ever say how big the object table is.
     Assume that the address of the first property block in the first object is
     the bottom of the object tree table. *)
  let count story =
    let (Object_tree_base table_start) = tree_base story
    let (Property_header table_end) = property_header_address story (Object 1)
    let entry_size = entry_size story
    (table_end - table_start) / entry_size

  (* The property entry begins with a length-prefixed zstring *)
  let name story n =
    let (Property_header addr) = property_header_address story n
    let length = Story.read_byte story (Byte_address addr)
    if length = 0 then "<unnamed>"
    else Zstring.read story (Zstring (addr + 1))

  (* Count down all the objects in the object table and record which ones have no parent. *)
  let roots story =
    let rec aux obj acc =
      let current = Object obj
      if current = invalid_object then
        acc
      else if (parent story current) = invalid_object then
        aux (obj - 1) (current :: acc)
      else
        aux (obj - 1) acc
    aux (count story) []

  let display_object_tree story =
    let rec aux acc indent obj =
      if obj = invalid_object then
        acc
      else
        let name = name story obj
        let child = child story obj
        let sibling = sibling story obj
        let object_text = Printf.sprintf "%s%s\n" indent name
        let with_object = acc + object_text
        let new_indent = "    " + indent
        let with_children = aux with_object new_indent child
        aux with_children indent sibling
    let to_string obj =
      aux "" "" obj
    accumulate_strings to_string (roots story)
