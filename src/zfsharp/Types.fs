module Types
  type bit_number = Bit_number of int
  type bit_size = Bit_size of int

  type byte_address = Byte_address of int
  type word_address = Word_address of int

  type IntMap<'T> = Collections.Map<int, 'T>

  type abbreviation_number = Abbreviation of int
  type abbrev_table_base = Abbreviation_table_base of int
  type word_zstring_address = Word_zstring of int
  type zstring_address = Zstring of int
  type zchar = Zchar of int