module Utility
  open Types

  let bit0 = Bit_number 0
  let bit1 = Bit_number 1
  let bit2 = Bit_number 2
  let bit3 = Bit_number 3
  let bit4 = Bit_number 4
  let bit5 = Bit_number 5
  let bit6 = Bit_number 6
  let bit7 = Bit_number 7
  let bit8 = Bit_number 8
  let bit9 = Bit_number 9
  let bit10 = Bit_number 10
  let bit11 = Bit_number 11
  let bit12 = Bit_number 12
  let bit13 = Bit_number 13
  let bit14 = Bit_number 14
  let bit15 = Bit_number 15

  let size1 = Bit_size 1
  let size2 = Bit_size 2
  let size3 = Bit_size 3
  let size4 = Bit_size 4
  let size5 = Bit_size 5

  let is_in_range (Byte_address address) size = 
    0 <= address && address < size
    
  let is_out_of_range address size =
    not (is_in_range address size)

  let fetch_bits (Bit_number high) (Bit_size length) word =
      let mask = ~~~(-1 <<< length)
      (word >>> (high - length + 1)) &&& mask

  let inc_byte_addr_by (Byte_address address) offset =
    Byte_address (address + offset)
  
  let dec_byte_addr_by address offset =
    inc_byte_addr_by address (0 - offset)

  let dereference_array address (bytes : byte[]) =
    if is_out_of_range address (Array.length bytes) then
      failwith "address out of range"
    else
      let (Byte_address addr) = address
      int bytes.[addr]

  let address_of_high_byte (Word_address address) =
    Byte_address address
  
  let address_of_low_byte (Word_address address) = 
    Byte_address (address + 1)

  let get_file filename =
    System.IO.File.ReadAllBytes filename