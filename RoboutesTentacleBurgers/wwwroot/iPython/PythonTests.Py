# ==========================================================
# PythonTests.py — Python Practice Sandbox
# ==========================================================

# ----------------------------------------------------------
# THE ACTUAL PRACTICE FUNCTIONS
# These are the solutions to practice questions.
# Each one is like a public static string Method() in C#
# ----------------------------------------------------------

def reverse_words(sentence):
    # .split() breaks the string into a list by spaces
    # "hello world" becomes ["hello", "world"]
    word_list = sentence.split()
    
    # .reverse() flips the list in place — no new list created
    # ["hello", "world"] becomes ["world", "hello"]
    word_list.reverse()
    
    # " ".join() glues the list back into a string with spaces
    # ["world", "hello"] becomes "world hello"
    return " ".join(word_list)


def reverse_words_oneline(sentence):
    # [::-1] means "read the list backwards" — slice notation
    # Same as .reverse() but works inline without a variable
    # sentence.split() gives the list, [::-1] flips it
    # " ".join() glues it back together
    return " ".join(sentence.split()[::-1])


def is_palindrome(s):
    s = s.lower()
    s = s.replace(" ","")
    return s == s[::-1]

# Q3

#def most_common_word(sentence):
#   sentence = sentence.mostcommon(1)
#   sentence = sentence.mostcommon(1)[0][0]
#   return " ".join(sentence)



from collections import Counter

def most_common_word(sentence):
    # Step 1 — split sentence into a list of words
    word_list = sentence.split()
    
    # Step 2 — Counter counts how many times each word appears
    # like {"the": 3, "cat": 1, "sat": 1}
    word_counts = Counter(word_list)
    
    # Step 3 — most_common(1) gets top 1 result as a list
    # [("the", 3)]  so [0][0] gets just the word "the"
    return word_counts.most_common(1)[0][0]
    

# Q4
def count_vowels(s):
    count = 0
    for letter in s:
        if letter in "aeiou":
            count += 1
    return count

# return sum(1 for letter in s if letter in "aeiou")    



# Q5 — input comes as string from the UI, convert to int first
def fizzbuzz(n):
    n = int(n)
    pass  # your code here






# Q6 — input comes as comma separated string like "1,2,2,3,3,4"
def remove_duplicates(items):
    num_list = [int(x) for x in items.split(",")]
    pass  # your code here

# Q7 — input comes as "word1 word2" — split on space to get both words
def is_anagram(words):
    parts = words.split()
    word1 = parts[0]
    word2 = parts[1]
    pass  # your code here

# Q8
def sum_of_digits(n):
    pass  # your code here

# Q9 — input comes as comma separated string
def flatten_list(items):
    pass  # your code here

# Q10 — input comes as comma separated string like "1,5,3,9,2"
def second_largest(nums):
    num_list = [int(x) for x in nums.split(",")]
    pass  # your code here


# ----------------------------------------------------------
# THE DISPATCHER — this is the part you asked about
# ----------------------------------------------------------
# In C# this would look like:
#   Dictionary<string, Func<string, string>> tests = new()
#   {
#       { "reverse_words", reverse_words },
#       { "reverse_words_oneline", reverse_words_oneline }
#   };
#
# Python does the same thing but the syntax is lighter.
# Functions in Python are just values — you can store them
# in a dictionary exactly like a string or a number.
# ----------------------------------------------------------

def run_test(function_name, *arguments):
    # This dictionary maps string names to actual functions
    # The function is stored as a VALUE — not called yet
    # Notice: reverse_words has no () after it
    # reverse_words   = storing the function itself
    # reverse_words() = calling the function right now
    available_tests = {
        "reverse_words":         reverse_words,
        "reverse_words_oneline": reverse_words_oneline,
        "is_palindrome":         is_palindrome,
        "most_common_word":      most_common_word,
        "count_vowels":          count_vowels,
        "fizzbuzz":              fizzbuzz,
        "remove_duplicates":     remove_duplicates,
        "is_anagram":            is_anagram,
        "sum_of_digits":         sum_of_digits,
        "flatten_list":          flatten_list,
        "second_largest":        second_largest,
    }

    # .get() looks up the name in the dictionary
    # Returns the function itself if found, None if not found
    # In C#: available_tests.TryGetValue(function_name, out var found_function)
    found_function = available_tests.get(function_name)

    # If we found a matching function, call it with the arguments
    # found_function(*arguments) unpacks the arguments and passes them in
    # *arguments means "however many arguments were passed, forward them all"
    # In C# this is like params string[] args
    if found_function:
        return str(found_function(*arguments))

    # If no match was found, return an error message
    return f"Unknown test: {function_name}"

