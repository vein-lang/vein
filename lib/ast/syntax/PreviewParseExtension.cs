namespace wave.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using static Sprache.Parse;
    using static Sprache.Position;
    using static Sprache.Result;

    public enum MemoFlags
    {
        NextFail
    }
    public static class PreviewParseExtension
    {
        // WAT THE F*CK, F*CK COMBINATORS, F*CK MY LIFE, F****CK!
        public static Parser<T> OrPreview<T>(this Parser<T> first, Parser<T> other) 
            where T : BaseSyntax, IPositionAware<T>, IPassiveParseTransition, new() => i =>
        {
            var fr = first(i);
            var sr = other(i);
            switch (fr.WasSuccessful)
            {
                case false when !sr.WasSuccessful && i.Memos.IsEnabled(MemoFlags.NextFail):
                    i.Memos.Disable(MemoFlags.NextFail);
                    return sr.IfFailure(sf => DetermineBestError(fr, sf));
                case true:
                    fr.Remainder.Memos.Enable(MemoFlags.NextFail);
                    break;
            }

            if (fr.WasSuccessful)
                return Success(fr.Value, fr.Remainder);
            if (sr.WasSuccessful)
                return Success(sr.Value, sr.Remainder);

            if (!i.IsEffort(fr, sr))
                return sr.IfFailure(sf => DetermineBestError(fr, sf));

            // read until terminator char
            var r = AnyChar.Until(Char(';'))(i);
            
            var bestResult = DetermineBestError(fr, sr);
            var error = new T();
            error.SetPos(FromInput(bestResult.Remainder), r.Remainder.Position - i.Position);

            
            error.Error = new PassiveParseError(bestResult.Message, bestResult.Expectations);
            r.Remainder.Memos.Enable(MemoFlags.NextFail);
            return Success(error, r.Remainder);
        };

        public static Parser<T> PreviewMultiple<T>(this Parser<T> first, params Parser<T>[] others)
            where T : BaseSyntax, IPositionAware<T>, IPassiveParseTransition, new() 
        {
            return i =>
            {
                var results = new[] {first}.Concat(others).Select(x => x(i)).ToArray();

                var succeeded = results.FirstOrDefault(x => x.WasSuccessful);

                if (succeeded is not null)
                    return Success(succeeded.Value, succeeded.Remainder);

                if (results.All(x => !x.WasSuccessful))
                {
                    if (i.Memos.IsEnabled(MemoFlags.NextFail))
                    {
                        i.Memos.Disable(MemoFlags.NextFail);
                        return DetermineBestErrors(results);
                    }
                }

                if (!i.IsEffort(results))
                    return DetermineBestErrors(results);

                var r = AnyChar.Until(Char(';'))(i);

                var error = new T();
                error.SetPos(FromInput(i), r.Remainder.Position - i.Position);

                var bestResult = DetermineBestErrors(results);
                error.Error = new PassiveParseError(bestResult.Message, bestResult.Expectations);
                r.Remainder.Memos.Enable(MemoFlags.NextFail);
                return Success(error, r.Remainder);
            };
        }


        private static bool IsEffort<T>(this IInput current, params IResult<T>[] attempts) 
            => attempts.Any(current.IsEffort);
        private static bool IsEffort<T>(this IInput current, IResult<T> attempt) 
            => current.Column != attempt.Remainder.Column || current.Line != attempt.Remainder.Line;

        private static bool IsEnabled(this IDictionary<object, object> value, MemoFlags flag) 
            => value.ContainsKey(flag);

        private static void Enable(this IDictionary<object, object> value, MemoFlags flag)
        {
            if (!value.ContainsKey(flag))
                value[flag] = flag;
        }
        private static void Disable(this IDictionary<object, object> value, MemoFlags flag)
        {
            if (value.ContainsKey(flag))
                value.Remove(flag);
        }
        private static IResult<T> IfFailure<T>(
            this IResult<T> result,
            Func<IResult<T>, IResult<T>> next)
        {
            if (result == null)
                throw new ArgumentNullException(nameof (result));
            return !result.WasSuccessful ? next(result) : result;
        }

        private static IResult<T> DetermineBestErrors<T>(params IResult<T>[] firstFailure) 
            => firstFailure.OrderByDescending(x => x.Remainder.Position).First();

        private static IResult<T> DetermineBestError<T>(
            IResult<T> firstFailure,
            IResult<T> secondFailure)
        {
            if (secondFailure.Remainder.Position > firstFailure.Remainder.Position)
                return secondFailure;
            return secondFailure.Remainder.Position == firstFailure.Remainder.Position ? 
                Failure<T>(firstFailure.Remainder, firstFailure.Message, firstFailure.Expectations.Union<string>
                    (secondFailure.Expectations)) : firstFailure;
        }
    }
}