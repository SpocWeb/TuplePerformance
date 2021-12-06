// ReSharper disable RedundantUsingDirective
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

// ReSharper restore RedundantUsingDirective

namespace org.structs.root.Extensions.maybe
{
	/// <summary> Allows to write Some.<see cref="Value{T}(T)"/> in addition to <see cref="MayBeError{T}.None"/> similar to F#</summary>
	public static class Some {
		public static MayBeError<T> Value<T>(T value) => new(value);
	}

	/// <summary> Implicit Cast to <see cref="Option{T}"/> and <see cref="Maybe{T}"/> saves specifying the Type </summary>
	public sealed class None
	{
		None(){}
		public static readonly None Value = new();
	}

	/// <summary> Same as <see cref="Maybe{T}"/>, only with <see cref="Error"/> Message. </summary>
	public readonly struct MayBeError<T> {

		public static readonly MayBeError<T> None = new();

		/// <summary> AKA 'Some' </summary>
		public T Value { get; }

		/// <summary> AKA 'None' </summary>
		public string? Error { get; }

		public MayBeError(T value, string? error = null) {
			Value = value;
			Error = error;
		}

		public MayBeError(string error) {
			Value = default!;
			Error = error;
		}

		public bool IsSome => Error is null;

	}

	/// <summary> AKA <see cref="Option{A}"/>, <see cref="Nullable{T}"/>; Contains either not null or an explaining Error Message for <see cref="Nullable"/> values </summary>
	/// <remarks>
	/// There is also the <see cref="Option{T}"/> class
	/// * which is 'safer', because it uses Inheritance
	/// * but heavier, because it uses the Heap.
	/// This Implementation is very similar to <see cref="Nullable{T}"/>.
	/// 
	/// This works analogous to NaN, continuing Processing without Exceptions, 
	/// which allows to write elegant, brief, functional Code, 
	/// but NaN makes it hard to detect when an Error was introduced! 
	/// This Class can track when the Null Value was introduced and why! 
	/// 
	/// More lightweight than Result{T}. 
	/// 
	/// Very fast, because the actual Error Message is only checked by Length and for special Constants. 
	/// The Message is structured for brevity and Memory Consumption: the first Character indicates the Severity
	/// 
	/// If you could add Type Parameters on Operator Overloads (not possible as of c# 4.0) you could even 
	/// define a Pipe or ?? Operator for the Get Operation to make it more concise, but at the Cost of Readability! 
	/// So far use the <see cref="Coalesce(T)"/> resp. <see cref="GetValue(T)"/> Method
	/// </remarks>
	[Serializable]
	//[UsedImplicitly]
	//[Immutable]
	public readonly struct Maybe<T> : IEquatable<Maybe<T>>, IEquatable<T>, IReadOnlyList<T> //, IOption<T>
	{

		#region static Operators

		public static EqualityComparer<T> EqualityComparer = EqualityComparer<T>.Default;

		/// <summary> AKA null; Flyweight </summary>
		public static Maybe<T> None(string error = Maybe.ERROR_NULL) => new(error);

		/// <summary> Factory Method </summary>
		public static Maybe<T> Some(T value) => new(value);

		/// <summary>Implements the operator ==.</summary>
		public static bool operator ==(Maybe<T> left, Maybe<T> right) => Equals(left, right); 

		/// <summary>Implements the operator <paramref name="left"/> != <paramref name="right"/>.</summary>
		public static bool operator !=(Maybe<T> left, Maybe<T> right) => !Equals(left, right); 

		/// <summary>Performs an explicit conversion of <paramref name="item"/> with Type <see cref="Maybe{T}"/> to <typeparamref name="T"/>.</summary>
		/// <exception cref="InvalidOperationException">when an Error is set or the Value is null</exception>
		public static explicit operator T(Maybe<T> item) => item.Value;

		/// <summary> implicit conversion from <typeparamref name="T"/> to <see cref="Maybe{T}"/>.</summary>
		/// <param name="item">The item.</param>
		/// <returns>The result of the conversion.</returns>
		/// <remarks>
		/// No need for calling <see cref="Maybe.Some{T}(T)"/> or a 'Some' Method, just convert implicitly. 
		/// </remarks>
		public static implicit operator Maybe<T>(T item) => new(item);
		public static implicit operator Maybe<T>(None _) => None();

		//Needs to be added as an Extension Method on T
		//public static Maybe<T> Binary<T>(this T self, Func<T, T, T> op, Maybe<T> that_); 

		/*// <summary>Performs an explicit conversion from <see cref="Maybe{T}"/> to <typeparamref name="T"/>.</summary>
		/// <param name="item">The item.</param>
		/// <returns>The result of the conversion.</returns>
		public static explicit operator T(Nullable<T> item) => item.Value; 
		*/

		public const string WarningLevel = "3"; 

		#endregion static Operators

		readonly T _Value;

		/// <summary>Gets the underlying value.</summary>
		/// <value>The value.</value>
		/// <exception cref="InvalidOperationException">when an Error is set or the Value is null</exception>
		public T Value {
			get {
				if (ReferenceEquals(Error, Maybe.ERROR_NULL)) {
					return default; //
				}
				if (_HasError) {
					throw new InvalidOperationException(Error);
				}
				return _Value;
			}
		}

		/// <summary>The concrete Error Message for this Value; Prefixed by Severity from 0 (OK) to 5 (Fatal)</summary>
		/// <remarks>
		/// propagated up from the Origin (Context is lost). 
		/// Use Result{T} to retain the Origin. 
		/// 
		/// Prefix it with <see cref="Exceptions.Error.Level"/> Severity to merge Errors from binary Operators using Max 
		/// 0 = OK
		/// 1 = Debug
		/// 2 = Verbose
		/// 3 = Trace
		/// 4 = Info
		/// 5 = Warning
		/// 7 = Error
		/// 9 = Fatal
		/// </remarks>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public string Error { get; }

		//Maybe() { Error = DefaultError; }

		/// <summary>Initializing Constructor, not publicly needed due to Conversion</summary>
		public Maybe(string error = Maybe.ERROR_NULL, T item = default!) {
			if ((Error = error) is null) { //ShouldNotBeNull()
				throw new ArgumentNullException(nameof(error),"Must not be null!");
			}
			if (error.Length <= 0 || error[0] > '9' || error[0] < '0') { //ShouldNotBeNull()
				throw new ArgumentOutOfRangeException(nameof(error), error, "Must start with a Severity from 0 to 9: " + error);
			}
			_Value = item!; //For Errors, accepts any Value
		}

		/// <summary>Initializing Constructor, not publicly needed due to Conversion</summary>
		public Maybe(T value)
			: this("0", value) {
			if (value is null) { //don't accept null!
				throw new ArgumentNullException(nameof(value));
			}
		}

		/// <summary> <returns><see cref="Value"/> when no Error, else one of the <paramref name="defaultValues"/></returns> </summary>
		/// <see cref="Coalesce(Func{T})"/>
		public T GetValue(Func<T> defaultValues) => _HasValue ? _Value : defaultValues();

		/// <summary> <returns><see cref="Value"/> when no Error, els <paramref name="fallBack"/></returns> </summary>
		/// <see cref="Coalesce(T)"/>
		public T GetValue(T fallBack) => _HasValue ? _Value : fallBack;

		/// <summary> <returns><see cref="Value"/> when no Error, else one of the <paramref name="defaultValues"/></returns> </summary>
		/// <see cref="GetValue(Func{T})"/>
		public T Coalesce(Func<T> defaultValues) => _HasValue ? _Value : defaultValues();

		/// <summary> <returns><see cref="Value"/> when no Error, els <paramref name="fallBack"/></returns> </summary>
		/// <see cref="GetValue(T)"/>
		public T Coalesce(T fallBack) => _HasValue ? _Value : fallBack;

		/// <summary>Applies the specified action to the value, if it is present.</summary>
		/// <param name="action">The action.</param>
		/// <returns>same instance for inlining</returns>
		public Maybe<T> Do(Action<T> action) {
			if (_HasValue) {
				action(_Value);
			}
			return this;
		}

		/// <summary>Executes <paramref name="action"/>, if the value is absent</summary>
		/// <returns>same instance for concatenation. </returns>
		public Maybe<T> Do(Action action) {
			if (_HasError) {
				action();
			}
			return this;
		}

		/// <summary>Flag whether this Instance has a Value </summary>
		/// <remarks><see cref="Error"/> has a Message with optional leading Severity with 0 being OK </remarks>
		public bool HasValue => _HasValue; 
		bool _HasValue => string.IsNullOrEmpty(Error) || Error[0] == '0'; 

		/// <summary>Flag whether this Instance has an Error, which may not be the same as having no Value </summary>
		public bool HasError => _HasError; 
		bool _HasError => !_HasValue; 

		/// <summary>Flag whether this Instance is valid, which is the same as having a Value</summary>
		public bool IsValid => _HasValue; //&& !HasError; 

		/// <summary>returns the given input if the input is not <see langword="null"/> and <see langword="true"/></summary>
		/// <param name="condition">the Condition do evaluate</param>
		/// <returns>input when it is not <see langword="null"/> and the condition is <see langword="true"/></returns>
		public Maybe<T> If(Func<T, bool> condition) => !_HasValue || condition(_Value) ? this
			: new Maybe<T>("false == " + condition.Method + "#" + condition.Target + "@" + _Value);

		/*// <summary>returns the given input if the input is not <see langword="null"/> and <see langword="true"/></summary>
		/// <param name="condition">the Condition do evaluate</param>
		/// <returns>input when it is not <see langword="null"/> and the condition is <see langword="true"/></returns>
		/// <remarks>yields a readable Expression, but cannot (yet) interpret it and Compilation is too expensive</remarks>
		public Maybe<T> If(Expression<Func<T, bool>> condition) {
			if (IsValid && !condition(_Value)) {
				return new Maybe<T>(default, condition + "@" + _Value);
			}
			return this;
		}*/

		/// <summary>returns the given input unless the input is not <see langword="null"/> and the condition is <see langword="false"/></summary>
		/// <param name="condition">the Condition do evaluate</param>
		/// <returns>input when it is not <see langword="null"/> and the condition is <see langword="false"/></returns>
		public Maybe<T> Unless(Func<T, bool> condition) 
			=> !_HasValue || !condition(Value) ? this
			: new Maybe<T>("true == " + condition.Method + "#" + condition.Target + "@" + _Value);

		/// <summary>Throws the exception IF this does not have value.</summary>
		/// <returns>actual value</returns>
		/// <exception cref="InvalidOperationException">if maybe does not have value</exception>
		public T Exception() => Exception(Invalid);
		public T Exception(Func<Exception> x) => _HasValue ? _Value : throw x();
		public T Exception(Exception x) => _HasValue ? _Value : throw x;

		InvalidOperationException Invalid() => new(Error);

		/// <summary>Throws an ArgumentException IF this does not have value.</summary>
		/// <returns>actual value</returns>
		/// <exception cref="InvalidOperationException">if maybe does not have value</exception>
		public T ArgumentException(string argName) => Exception(new ArgumentException(Error, argName));

		/// <summary>Combines this optional with the pipeline function</summary>
		/// <typeparam name="TTarget">The type of the target.</typeparam>
		/// <param name="evaluator">The combinator (pipeline function)</param>
		/// <returns>optional result</returns>
		/// <remarks>could also be named 'Call'</remarks>
		public Maybe<TTarget> Get<TTarget>(Func<T, Maybe<TTarget>> evaluator) 
			=> _HasValue ? evaluator(_Value) : new Maybe<TTarget>(Error);

		/// <summary>Converts this instance to <see cref="Maybe{T}"/>, 
		/// while applying <paramref name="evaluator"/> if there is a value.
		/// </summary>
		/// <typeparam name="TTarget">The type of the target.</typeparam>
		/// <param name="evaluator">The converter.</param>
		/// <returns></returns>
		public Maybe<TTarget> Get<TTarget>(Func<T, TTarget> evaluator) 
			=> _HasValue ? evaluator(_Value) : new Maybe<TTarget>(Error);

		/// <summary>Retrieves converted value, using a <paramref name="fallBack"/> if it is absent.</summary>
		/// <typeparam name="TTarget">type of the conversion target</typeparam>
		/// <param name="converter">The converter.</param>
		/// <param name="fallBack">The default value.</param>
		/// <returns>value</returns>
		public TTarget Get<TTarget>(Func<T, TTarget> converter, Func<TTarget> fallBack) 
			=> _HasValue ? converter(_Value) : fallBack();

		/// <summary>Retrieves converted value, using a <paramref name="fallBack"/> if it is absent.</summary>
		/// <typeparam name="TTarget">type of the conversion target</typeparam>
		/// <param name="converter">The converter.</param>
		/// <param name="fallBack">The default value.</param>
		/// <returns>value</returns>
		public TTarget Get<TTarget>(Func<T, TTarget> converter, TTarget fallBack) 
			=> _HasValue ? converter(_Value) : fallBack;

		/// <summary>Determines whether the specified <see cref="object"/> is equal to the current <see cref="object"/>.</summary>
		/// <param name="that">The <see cref="object"/> to compare with the current <see cref="object"/>.</param>
		/// <returns><see langword="true"/> if the specified <see cref="object"/> is equal to the current <see cref="object"/>; otherwise, <see langword="false"/>.</returns>
		public override bool Equals(object? that) => that switch {
			Maybe<T> maybe => Equals(maybe),
			T variable => variable.Equals(_Value), //null is only of type 'object', so no null check necessary!
			_ => false
		};

		/// <summary>Serves as a hash function for this instance.</summary>
		/// <returns>A hash code for the current <see cref="Maybe{T}"/>.</returns>
		public override int GetHashCode() => _HasValue ? _Value?.GetHashCode() ?? 0 : 0;

		/// <summary>Determines whether the specified <see cref="Maybe{T}"/> is equal to the current <see cref="Maybe{T}"/>.</summary>
		/// <param name="that">The <see cref="Maybe{T}"/> to compare with.</param>
		/// <returns><see langword="true"/> if the objects are equal</returns>
		/// <remarks>
		/// the actual Error Messages may differ though!
		/// </remarks>
		public bool Equals(T? that) {
			if (_HasValue ||
				Error.ReferenceEquals(Maybe.ERROR_NAN) ||
				Error.ReferenceEquals(Maybe.ERROR_NULL)) {
				return EqualityComparer.Equals(_Value, that);
			}
			return false; 
		}

		/// <summary>Determines whether the specified <see cref="Maybe{T}"/> is equal to the current <see cref="Maybe{T}"/>.</summary>
		/// <param name="that">The <see cref="Maybe{T}"/> to compare with.</param>
		/// <returns><see langword="true"/> if the objects are equal</returns>
		/// <remarks>
		/// the actual Error Messages may differ though!
		/// </remarks>
		public bool Equals(Maybe<T> that) {
			if (_HasValue ||
				Error.ReferenceEquals(Maybe.ERROR_NAN) ||
				Error.ReferenceEquals(Maybe.ERROR_NULL)) {
				return that.Equals(_Value);
			}
			if (ReferenceEquals(Maybe.DefaultError, Maybe.ERROR_NAN)) {
				return false; //NaN != NaN
			}
			if (ReferenceEquals(Maybe.DefaultError, Maybe.ERROR_NULL)) {
				return true; //null is null
			}
			return that.Error == Error;
		}

		/// <summary>Generic Implementation of a binary Operator (also for non-commutative)</summary>
		public Maybe<T> Unary(Func<T, T> op) => Get(op); 

		/// <summary>Generic Implementation of a binary Operator (also for non-commutative)</summary>
		public Maybe<T> Binary(Func<T, T, T> op, Maybe<T> that) {
			//return Get(i => that_.Binary(op, i)); //commuted! 
			//return Get(i => i.Binary(op, that)); //use extension Method to avoid Commutation!
			if (_HasValue) {
				return _Value.Binary(op, that);
			}
			if (that._HasValue) {
				return Binary(op, that._Value);
			}
			string maxError = string.Compare(Error, that.Error, StringComparison.Ordinal) > 0 //propagate the more severe Error
							? Error : that.Error; 
			if (string.Compare(maxError, WarningLevel, StringComparison.Ordinal) > 0) {
				return new Maybe<T>(Error);
			}
			return new Maybe<T>(Error, op(_Value, that._Value));
		}

		/// <summary>Generic Implementation of a binary Operator (also for non-commutative)</summary>
		public Maybe<T> Binary(Func<T, T, T> op, T that) => Get(i => op(i, that)); 

		/// <summary>downcast to object is not allowed; the Boxing Behavior of <see cref="Nullable"/> to <see langword="null"/> is specially built into the Runtime!</summary>
		/// <returns>
		/// downcast to object is not allowed; the Boxing Behavior of <see cref="Nullable"/> to <see langword="null"/> is specially built into the Runtime!
		/// </returns>
		public object? ToObject() {
			if (_HasValue) {
				return _Value; //may be boxed
			}
			if (ReferenceEquals(Error, Maybe.ERROR_NULL)) {
				return null;
			}
			if (ReferenceEquals(Error, Maybe.ERROR_EXCEPTION)) {
				throw new InvalidOperationException(Error);
			}
			/*if (ReferenceEquals(Error, ERROR_NAN)) {
				return NaN;
			}*/
			return this; //will be boxed
		}

		/*//TODO: <summary>Converts maybe into result, using the specified error as the failure</summary>
		/// <returns>result describing current maybe</returns>
		public Result<T> ToResult(Logg.Levels level) {
			if (HasValue) {
				return _Value;
			}
			return new Result<T>().e(Error, level);
		}*/

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => _HasValue ? "" + _Value : "<" + Error + ">";

		public T GetSome() => _Value;

		public bool IsNone => !IsSome; //

		public bool IsSome => _HasValue;

		public void Match(Action<T>? f = null, Action? h = null) {
			if (_HasValue) {
				f?.Invoke(_Value);
			} else {
				h?.Invoke();
			}
		}

		public U Match<U>(Func<T, U> f, Func<U> fallBack) => _HasValue ? f(_Value) : fallBack();
		public U Match<U>(Func<T, U> f, U fallBack) => _HasValue ? f(_Value) : fallBack;
		public U Match<U>(U ifValid, U fallBack) => _HasValue ? ifValid : fallBack;

		public Maybe<U> Match<U>(Func<T, U> f) => _HasValue ? f(_Value) : Maybe<U>.None();
		public Maybe<U> Match<U>(Func<T, Maybe<U>> f) => _HasValue ? f(_Value) : Maybe<U>.None();
		public Maybe<U> Match<U>(Func<T, Maybe<U>> f, Maybe<U> u) => _HasValue ? f(_Value) : u;

		public T IfNone(T fallBack) => _HasValue ? _Value : fallBack;
		public T IfNone(Func<T> fallBack) => _HasValue ? _Value : fallBack();

		/// <summary> AKA <see cref="Maybe.Bind"/>; executes <paramref name="f"/> on <see cref="Value"/> only when it's set </summary>
		public void IfSomeDo(Action<T> f) {
			if (_HasValue) {
				f(_Value);
			}
		}

		/// <summary> Enables to use Query Syntax </summary>
		/// <example>
		/// <code>
		/// from maybeX
		/// from maybeY
		/// select maybeX.Plus(maybeY)
		/// </code>
		/// </example>
		public Maybe<TResult> Select<TResult>(Func<T, TResult> selector)
			=> Match(x => Maybe<TResult>.Some(selector(x)), Maybe<TResult>.None());

		/// <summary> 'Flattens' this Maybe with the <paramref name="selector"/> Maybe </summary>
		/// <example>
		/// <code>
		/// </code>
		/// </example>
		public Maybe<TResult> SelectMany<TResult>(Func<T, Maybe<TResult>> selector) => Match(selector, Maybe<TResult>.None());

		#region Implementation of IReadOnlyCollection<out T>

		public int Count => _HasValue ? 1 : 0;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<T> GetEnumerator() { if (_HasValue) { yield return _Value; } }

		public T this[int index] => _HasValue && index == 0 ? _Value
			: throw new IndexOutOfRangeException(index + " >= " + Count);

		#endregion

	}

	/// <summary> Extension Operator Methods returning Arithmetic <see cref="Maybe{Int32}"/> Monads for int </summary>
	static partial class Maybe {

		#region static Constants 

		/// <summary> Exhibits ANSI SQL-like Null Behavior for <see cref="Maybe{T}"/>, i.e. NaN != NaN </summary>
		/// <remarks>
		/// const allows for Optimizations.
		/// Allows to track many empty Items in a Dictionary or DB Table. 
		/// Each Error will be made individual by a Guid and a Stack Trace. 
		/// Even the same Error Messages are tracked individually and 
		/// </remarks>
		public const string ERROR_NAN = "NaN";

		/// <summary> Exhibits SQL-Server like Null Behavior for <see cref="Maybe{T}"/>, i.e. Null == Null and x + Null == x </summary>
		/// <remarks>
		/// also falls back gracefully to <see langword="null"/> or special Values for Value Types instead of throwing an Exception. 
		/// Different Error Messages are collapsed into a single Error (first wins)
		/// </remarks>
		public const string ERROR_NULL = "Null";

		/*// <summary>Exhibits SQL-Server like Null Behavior, i.e. Null == Null and x + Null == x</summary>
		/// <remarks>
		/// also falls back gracefully to <see langword="null"/> or special Values for Value Types instead of throwing an Exception
		/// </remarks>
		public const string ERROR_DEFAULT = "Default";
		*/

		/// <summary>Exhibits fail-fast Null Behavior, i.e. any Operation on this will throw an Exception</summary>
		public const string ERROR_EXCEPTION = "Exception";

		/*// <summary>Flag to consider same or different Error Messages as always same, specific or never</summary>
		/// <remarks>
		/// should be consistent with <see cref="DefaultError"/>: 
		/// IS_ERROR_SPECIFIC == true  = DefaultError == NaN
		/// IS_ERROR_SPECIFIC == false = DefaultError == Null
		/// </remarks>
		public static bool? IS_ERROR_SPECIFIC =  true;
		*/
		///// <summary>Fast empty Instance</summary>
		///// <remarks>
		///// does NOT help in detecting Errors!
		///// does NOT prevent using the empty Constructor!
		///// </remarks>
		//public static Maybe<T> Empty = new Maybe<T>(DefaultError);

		/// <summary>The Default Error for empty Instances</summary>
		/// <remarks>
		/// a static Member allows to control individual Default Behavior for each Type T
		/// Null != Null
		/// Alternative Behavior: 
		/// * Null + x == Null (NaN => Any ; Code easier to read, but Errors harder to find!)
		/// * Null + x == x    (Sql => Each; )
		/// * Null + x => Exception (early Detection)
		/// Null || <see langword="true"/> == <see langword="true"/>
		/// Null &amp;&amp; <see langword="false"/> == <see langword="false"/>
		/// </remarks>
		public static string DefaultError = ERROR_NAN; //"This Value is empty!";

		#endregion //static Constants 

		/// <summary> Factory/Conversion Extension Method to exploit Type Inference </summary>
		public static Maybe<T> Some<T>(T value) => Maybe<T>.Some(value);

		/// <summary> Factory/Conversion Extension Method to exploit Type Inference </summary>
		public static Maybe<T> AsMaybe<T>(this T value) => Maybe<T>.Some(value);

		///<summary>Transforms non-null values and propagates nulls.</summary>
		/// <remarks> this is briefer using ?.projection()</remarks>
		public static Maybe<TOut> SelectMaybe<TIn, TOut>(this TIn? nullable
			, Func<TIn, TOut> projection)
			where TIn : struct => nullable.HasValue ? projection((TIn)nullable) : Maybe<TOut>.None();

		public static Maybe<TResult> Map<T, TResult>(this Maybe<T> option, Func<T, TResult> map) 
			=> option.HasValue ? map(option.Value) : None.Value;

		/// <summary> AKA When, Where; returns <see cref="None"/> when <paramref name="predicate"/> is false </summary>
		public static Maybe<T> AsMaybe<T>(this T value, Func<T, bool> predicate) 
			=> predicate(value) ? value : None.Value;

		public static T Reduce<T>(this Maybe<T> option, T whenNone) 
			=> option.HasValue ? option.Value : whenNone;

		public static T Reduce<T>(this Maybe<T> option, Func<T> whenNone) 
			=> option.HasValue ? option.Value : whenNone();

		/// <summary>Generic Implementation of a binary Operator (also for non-commutative <paramref name="op"/>s)</summary>
		public static Maybe<T> Binary<T>(this T first, Func<T, T, T> op, Maybe<T> second) 
			=> second.Get(i => op(first, i));

		#region Option Arithmetic

		/// <summary>adds <paramref name="y"/> to <paramref name="x"/> </summary>
		public static Maybe<int> Plus(this Maybe<int> x, Maybe<int> y) => x.Get(i => y.Plus(i)); 

		/// <summary>adds <paramref name="y"/> to <paramref name="x"/> </summary>
		public static Maybe<int> Plus(this Maybe<int> x, int y) => x.Get(i => i + y); 

		/// <summary>adds <paramref name="y"/> to <paramref name="x"/> </summary>
		public static int Plus(this int x, int y) => x + y; 

		/// <summary>adds <paramref name="y"/> to <paramref name="x"/> </summary>
		public static Maybe<int> Plus(this int x, Maybe<int> y) => y.Get(i => i + x); 

		/// <summary>adds <paramref name="y"/> to <paramref name="x"/> </summary>
		public static int Minus(this int x, int y) => x + y; 

		/// <summary>subtracts <paramref name="subtrahend"/> from <paramref name="minuend"/> </summary>
		public static Maybe<int> Minus(this Maybe<int> minuend, Maybe<int> subtrahend) => minuend.Binary(Minus, subtrahend); 

		/// <summary>subtracts <paramref name="subtrahend"/> from <paramref name="minuend"/> </summary>
		public static Maybe<int> Minus(this Maybe<int> minuend, int subtrahend) => minuend.Binary(Minus, subtrahend); 

		/// <summary>subtracts <paramref name="subtrahend"/> from <paramref name="minuend"/> </summary>
		public static Maybe<int> Minus(this int minuend, Maybe<int> subtrahend) => minuend.Binary(Minus, subtrahend);

		/// <summary>multiplies <paramref name="y"/> with <paramref name="x"/> </summary>
		public static int Times(this int x, int y) => x * y; 

		/// <summary>multiplies <paramref name="y"/> with <paramref name="x"/> </summary>
		public static Maybe<int> Times(this Maybe<int> x, Maybe<int> y) => x.Binary(Times, y); 

		/// <summary>multiplies <paramref name="y"/> with <paramref name="x"/> </summary>
		public static Maybe<int> Times(this Maybe<int> x, int y) => x.Binary(Times, y); 

		/// <summary>multiplies <paramref name="y"/> with <paramref name="x"/> </summary>
		public static Maybe<int> Times(this int x, Maybe<int> y) => x.Binary(Times, y);

		/// <summary>Divides <paramref name="x"/> by <paramref name="y"/> </summary>
		public static int Per(this int x, int y) => x / y; 

		/// <summary>Divides <paramref name="x"/> by <paramref name="y"/> </summary>
		public static Maybe<int> Per(this Maybe<int> x, Maybe<int> y) => x.Binary(Per, y); 

		/// <summary>Divides <paramref name="x"/> by <paramref name="y"/> </summary>
		public static Maybe<int> Per(this Maybe<int> x, int y) => x.Binary(Per, y); 

		/// <summary>Divides <paramref name="x"/> by <paramref name="y"/> </summary>
		public static Maybe<int> Per(this int x, Maybe<int> y) => x.Binary(Per, y);

		#endregion Option Arithmetic

		#warning make this Default Implementations: 

		/// <summary>compares the given Objects</summary>
		public static bool ReferenceEquals<T>(this T? value, T? that) where T : class => object.ReferenceEquals(value, that);

	}

}
