// See https://aka.ms/new-console-template for more information


using System.Runtime.CompilerServices;
using U8Str;

u8str str1 = "Hello, World!"u8;//No allocation
u8str str2 = "Hello, World!"u8;//No allocation
u8str str3 = u8str.FromArrayNoCopy("Hello, World!"u8.ToArray());//No additional allocation

//u8str str4 = "Hello, World!"u8[1..];//Error U8STR001  Only utf8 string literals can be converted to u8str

Console.WriteLine(Unsafe.AreSame(in str1[0], in str2[0]));
//True
Console.WriteLine(str1==str2);
//True
Console.WriteLine(str1==str3);
//True
Console.WriteLine(str1=="Hello, World!"u8);
//True
Console.WriteLine(str1);
//Hello, World!
Console.WriteLine(str1[..5]);
//Hello
Console.WriteLine(str2+str3[6..^1]);
//Hello, World! World

var dict = new Dictionary<u8str, int>();
