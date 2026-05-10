using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Services.Parser
{
    public record ParseError(string Message, int Position);

    /// <param name="Value">パース結果。エラー時は途中までの結果が入る。</param>
    /// <param name="Error">エラー情報。成功時は null。</param>
    public record ParseResult<T>(T? Value, ParseError? Error)
    {
        public bool IsOk => Error is null;

        /// <summary>成功。</summary>
        public static ParseResult<T> Ok(T value) => new(value, null);

        /// <summary>失敗。途中結果あり。</summary>
        public static ParseResult<T> Fail(T partial, string msg, int pos) => new(partial, new(msg, pos));

        /// <summary>失敗。何も得られなかった場合。</summary>
        public static ParseResult<T> Fail(string msg, int pos) => new(default, new(msg, pos));
    }
}
