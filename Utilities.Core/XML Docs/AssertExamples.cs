String s = null;
        
// The default is to apply <see cref="Utilities.Core.StringAssertion.All">StringAssertion.All</see>.
// An <see cref="System.ArgumentNullException">ArgumentNullException</see> will be raised because s is null.
s.Check("s");

s = "";

// An <see cref="System.ArgumentNullException">ArgumentException</see> will be raised because s is empty.
s.Check("s");

s = "   ";

// An <see cref="System.ArgumentNullException">ArgumentException</see> will be raised because s consists only of whitespace.
s.Check("s");

// No exception will be raised because, even though
// s consists only of whitespace, it has a length
// greater than zero.
s.Check("s", StringAssertion.NotNull | StringAssertion.NotZeroLength);

s = "123";

// An <see cref="System.ArgumentNullException">ArgumentException</see> will be raised.
// Because <see cref="Utilities.Core.StringAssertion.None">StringAssertion.None</see> is specified,
// s will not be checked for null-ness, zero length or if it consists only of whitespace.
// Instead, s will be checked to see if its length is 5, which fails because
// s is only 3 characters long.
s.Check("s", StringAssertion.None, 5);

// The last two Int32 parameters are the minimum and
// maximum allowed length (inclusive) of s.
// Because s is 3 characters long, no exception will be raised.
s.Check("s", StringAssertion.None, 3, 5);

// Other types can be checked for null-ness with the CheckForNull<T> method.
StreamReader sr = null;
sr.CheckForNull("sr");


