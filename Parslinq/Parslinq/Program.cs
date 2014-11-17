using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parslinq
{
    class Program
    {


        static void Main(string[] args)
        {
            // entry point for parsing
            var next_char = ParserMonad.Item<char>();

            // extract digit
            var digit = from c in next_char
                        where Char.IsDigit(c)
                        select Int32.Parse(c.ToString());

            // interpret + and *
            var add = from left in digit
                      from op in next_char
                      from right in digit
                      where op == '+' || op == '*'
                      select op == '+' ? left + right : left * right;

            foreach (int result in add.Parse("1+2"))
                Console.WriteLine(result);

            foreach (int result in add.Parse("2*3"))
                Console.WriteLine(result);

            Console.ReadLine();
        }
    }
}
