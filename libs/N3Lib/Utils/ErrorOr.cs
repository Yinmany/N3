namespace N3Lib
{
    public readonly struct ErrorOr<T>
    {
        public readonly int ErrorCode;
        public readonly T Value;

        public ErrorOr(int value)
        {
            this.Value = default!;
            this.ErrorCode = value;
        }

        public ErrorOr(T data)
        {
            this.Value = data;
            this.ErrorCode = 0;
        }

        public bool IsError => ErrorCode != 0;

        public void Deconstruct(out int err, out T value)
        {
            err = ErrorCode;
            value = Value;
        }

        public static implicit operator ErrorOr<T>(T value)
        {
            return new ErrorOr<T>(value);
        }

        public static implicit operator ErrorOr<T>(int errorCode)
        {
            return new ErrorOr<T>(errorCode);
        }
    }

    public readonly struct ErrorOr<TError, TResult> where TError : class
    {
        public readonly TError Error;
        public readonly TResult Value;

        public ErrorOr(TError value)
        {
            this.Value = default!;
            this.Error = value;
        }

        public ErrorOr(TResult data)
        {
            this.Value = data;
            this.Error = null;
        }

        public bool IsError => Error != null;

        public void Deconstruct(out TError err, out TResult value)
        {
            err = Error;
            value = Value;
        }

        public static implicit operator ErrorOr<TError, TResult>(TResult value)
        {
            return new ErrorOr<TError, TResult>(value);
        }

        public static implicit operator ErrorOr<TError, TResult>(TError error)
        {
            return new ErrorOr<TError, TResult>(error);
        }
    }
}
