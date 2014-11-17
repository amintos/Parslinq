using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parslinq
{

    public class ParseState<S>
    {
        private IEnumerator<S> Source;
        private bool Resolved = false;
        //private bool ItemsLeft = false;
        private ParseState<S> CachedNext = null;

        public S Value {get; private set;}

        public ParseState(IEnumerator<S> source, bool first)
        {
            Source = source;
            if (first) source.MoveNext();
            Value = source.Current;
        }

        public ParseState(IEnumerator<S> source) : this(source, true) { }

        public ParseState(IEnumerable<S> source) : this(source.GetEnumerator()) {}

        public ParseState<S> Next {
            get {
                if (!Resolved) {
                    CachedNext = Source.MoveNext() ? new ParseState<S>(Source, false) : null;
                    Resolved = true;
                }
                return CachedNext;
            }
        }

    }

    public class ParseResult<S, T>
    {
        public ParseState<S> State {get; internal set;}
        public T Value {get; internal set;}

        public static implicit operator T(ParseResult<S, T> result)
        {
            return result.Value;
        }
    }

    public static class ParserMonad
    {
        public static Parser<S, T> AsParser<S, T>(this T identity)
        {
            return new UnitParser<S, T>(identity);
        }

        public static Parser<S, U> SelectMany<S, T, U>(
            this Parser<S, T> parser, 
            Func<T, Parser<S, U>> func)
        {
            return new MonadParser<S, T, U>(parser, func);
        }

        public static Parser<S, V> SelectMany<S, T, U, V>(
            this Parser<S, T> parser, 
            Func<T, Parser<S, U>> k, 
            Func<T, U, V> s)
        {
            return parser.SelectMany(x => 
                k(x).SelectMany(y => 
                    new UnitParser<S, V>(s(x, y))));
        }

        public static Parser<S, T> Where<S, T>(
            this Parser<S, T> parser, 
            Func<T, bool> predicate)
        {
            return new FilterParser<S, T>(parser, predicate);
        }

        public static Parser<S, U> Select<S, T, U>(this Parser<S, T> parser, Func<T, U> func) 
        {
            return parser.SelectMany(x => new UnitParser<S, U>(func(x)));
        }

        public static Parser<U, U> Item<U>()
        {
            return new ItemParser<U>();
        }


    }

    public abstract class Parser<S, T> 
    {
        public abstract IEnumerable<ParseResult<S, T>> Parse(ParseState<S> input);

        public IEnumerable<ParseResult<S, T>> Parse(IEnumerable<S> input)
        {
            return Parse(new ParseState<S>(input));
        }


        public static Parser<S, T> operator | (Parser<S, T> left, Parser<S, T> right)
        {
            return new BranchingParser<S, T>(left, right);
        }

 
    }

    public class UnitParser<S, T> : Parser<S, T>
    {
        private T Identity;

        public UnitParser(T identity)
        {
            Identity = identity;
        }
        public override IEnumerable<ParseResult<S, T>> Parse(ParseState<S> input)
        {
            yield return new ParseResult<S, T>() { 
                Value = Identity, 
                State = input };
        }
    }

    public class NullParser<S, T> : Parser<S, T>
    {
        public override IEnumerable<ParseResult<S, T>> Parse(ParseState<S> input)
        {
            yield break;
        }
    }

    public class StopParser<S, T> : Parser<S, T>
    {
        public override IEnumerable<ParseResult<S, T>> Parse(ParseState<S> input)
        {
            if (input.Next == null)
            {
                yield return new ParseResult<S, T>
                {
                    Value = default(T),
                    State = input
                };
            }
        }
    }

    public class ItemParser<S> : Parser<S, S>
    {
        public override IEnumerable<ParseResult<S, S>> Parse(ParseState<S> input)
        {
            yield return new ParseResult<S, S>()
            {
                Value = input.Value,
                State = input.Next
            };
        }
    }


    public class BranchingParser<S, T> : Parser<S, T>
    {
        private Parser<S, T> First;
        private Parser<S, T> Second;

        public BranchingParser(Parser<S, T> first, Parser<S, T> second)
        {
            First = first;
            Second = second;
        }

        public override IEnumerable<ParseResult<S, T>> Parse(ParseState<S> input)
        {
            foreach (ParseResult<S, T> result in First.Parse(input))
            {
                yield return result;
            }
            foreach (ParseResult<S, T> result in Second.Parse(input))
            {
                yield return result;
            }
        }
    }

    public class FilterParser<S, T> : Parser<S, T>
    {
        private Parser<S, T> Input;
        private Func<T, bool> Predicate;

        public FilterParser(Parser<S, T> input, Func<T, bool> predicate)
        {
            Input = input;
            Predicate = predicate;
        }

        public override IEnumerable<ParseResult<S, T>> Parse(ParseState<S> input)
        {
            foreach (ParseResult<S, T> result in Input.Parse(input))
            {
                if (Predicate(result.Value))
                {
                    yield return result;
                }
            }
        }
    }

    public class MonadParser<S, T, U> : Parser<S, U>
    {
        private Parser<S, T> Source;
        private Func<T, Parser<S, U>> Continuation;

        public MonadParser(Parser<S, T> source, Func<T, Parser<S, U>> continuation)
        {
            Source = source;
            Continuation = continuation;
        }

        public override IEnumerable<ParseResult<S, U>> Parse(ParseState<S> input)
        {
            foreach (ParseResult<S, T> first in Source.Parse(input))
            {
                Parser<S, U> next = Continuation(first.Value);
                foreach (ParseResult<S, U> second in next.Parse(first.State)) 
                {
                    yield return second;
                }
            }
        }
    }
}
