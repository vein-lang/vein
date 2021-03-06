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
            
            var error = new T();
            error.SetPos(FromInput(i), r.Remainder.Position - i.Position);

            var bestResult = DetermineBestError(fr, sr);
            error.Error = new PassiveParseError(bestResult.Message, bestResult.Expectations);
            r.Remainder.Memos.Enable(MemoFlags.NextFail);
            return Success(error, r.Remainder);
        };


        private static bool IsEffort<T>(this IInput current, params IResult<T>[] attempts) 
            => attempts.Any(current.IsEffort);
        private static bool IsEffort<T>(this IInput current, IResult<T> attempt) 
            => current.Column != attempt.Remainder.Column || current.Line != attempt.Remainder.Line;

        internal static bool IsEnabled(this IDictionary<object, object> value, MemoFlags flag) 
            => value.ContainsKey(flag);

        internal static void Enable(this IDictionary<object, object> value, MemoFlags flag)
        {
            if (!value.ContainsKey(flag))
                value[flag] = flag;
        }
        internal static void Disable(this IDictionary<object, object> value, MemoFlags flag)
        {
            if (value.ContainsKey(flag))
                value.Remove(flag);
        }

        public static IInput AdvancedDelta(this IInput ii, params char[] terminators)
        {
            if (ii.AtEnd)
                throw new InvalidOperationException("The input is already at the end of the source.");
            
            while (!terminators.Contains(ii.Current))
            {
                if (ii.AtEnd)
                    throw new Exception();
                ii = ii.Advance();
            }

            return ii;
        }

        public static IResult<T> IfFailure<T>(
            this IResult<T> result,
            Func<IResult<T>, IResult<T>> next)
        {
            if (result == null)
                throw new ArgumentNullException(nameof (result));
            return !result.WasSuccessful ? next(result) : result;
        }

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

        public static IInput AdvancedDelta(this IInput ii, int delta)
        {
            if (ii.AtEnd)
                throw new InvalidOperationException("The input is already at the end of the source.");
            if (delta <= 0)
                throw new InvalidOperationException($"The input can't advance by {delta}, need a positive number.");
            if (ii.Position + delta > ii.Source.Length)
                throw new InvalidOperationException($"The input can't advance by {delta} because it exceeds the bounds of the source.");

            var line = ii.Line;
            var column = ii.Column;

            for (var i = ii.Position; i < ii.Position + delta; i++)
            {
                if (ii.Source[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return new WaveInput(ii.Source, ii.Position + delta, line, column);
        }
    }
}