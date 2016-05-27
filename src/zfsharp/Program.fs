// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

module Main
  open Types

  let fetch_bits (Bit_number high) (Bit_size length) word =
      let mask = ~~~(-1 <<< length)
      (word >>> (high - length + 1)) &&& mask

  [<EntryPoint>]
  let main argv = 
      printfn "%A" (fetch_bits (Bit_number 7) (Bit_size 2) 64)
      0 // return an integer exit code
