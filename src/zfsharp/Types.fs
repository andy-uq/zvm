module Types
  type bit_number = Bit_number of int
  type bit_size = Bit_size of int

  type byte_address = Byte_address of int
  type word_address = Word_address of int

  type IntMap<'T> = Collections.Map<int, 'T>
