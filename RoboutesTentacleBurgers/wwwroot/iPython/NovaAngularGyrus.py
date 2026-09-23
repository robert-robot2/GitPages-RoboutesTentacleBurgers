# NovaAngularGyrus.py

import json
import re
import math
import random

CONCEPT_DEMOS = {
    "indexing": lambda: (
        lambda x: {
            "explain": (
                f"x = {x}\n"
                f"x[0]  = {x[0]}   ← first item (index starts at 0)\n"
                f"x[1]  = {x[1]}   ← second item\n"
                f"x[-1] = {x[-1]}  ← LAST item (negative counts from end)\n"
                f"x[-2] = {x[-2]}  ← second to last\n"
                f"Rule: x[-1] always = last item, x[-2] = second to last"
            )
        }
    )([10, 20, 30, 40, 50]),

    "slicing": lambda: (
        lambda x: {
            "explain": (
                f"x = {x}\n"
                f"x[1:]   = {x[1:]}   ← skip first, take rest\n"
                f"x[:3]   = {x[:3]}   ← first 3 (stop is exclusive)\n"
                f"x[1:4]  = {x[1:4]}  ← index 1,2,3\n"
                f"x[::2]  = {x[::2]}  ← every 2nd item\n"
                f"x[::-1] = {x[::-1]} ← reversed"
            )
        }
    )([1, 2, 3, 4, 5, 6, 7]),

    "comprehension": lambda: (
        lambda x: {
            "explain": (
                f"x = {x}\n"
                f"[n*2 for n in x]          = {[n*2 for n in x]}\n"
                f"[n for n in x if n%2==0]  = {[n for n in x if n%2==0]}  ← evens only\n"
                f"[n**2 for n in x]         = {[n**2 for n in x]}  ← squares\n"
                f"Compact loop in one line."
            )
        }
    )([1, 2, 3, 4, 5]),

    "enumerate": lambda: (
        lambda x: {
            "explain": (
                f"x = {x}\n"
                f"list(enumerate(x)) = {list(enumerate(x))}\n"
                + "\n".join(f"  i={i}  v={v}" for i, v in enumerate(x)) +
                f"\nUse when you need both index AND value."
            )
        }
    )(["alpha", "bravo", "charlie"]),

    "zip": lambda: (
        lambda a, b: {
            "explain": (
                f"a = {a}\n"
                f"b = {b}\n"
                f"list(zip(a,b)) = {list(zip(a,b))}\n"
                f"dict(zip(a,b)) = {dict(zip(a,b))}  ← instant dict!"
            )
        }
    )(["name", "class", "hp"], ["Nova", "Assassin", 350]),

    "dictionary": lambda: (
        lambda d: {
            "explain": (
                f"d = {d}\n"
                f"d['nova']       = {d['nova']}\n"
                f"d.get('x', 0)  = {d.get('x', 0)}  ← safe get with default\n"
                f"d.keys()   = {list(d.keys())}\n"
                f"d.values() = {list(d.values())}\n"
                f"Lookup is O(1) — same speed for 3 or 3,000,000 items."
            )
        }
    )({"nova": 350, "player": 100, "enemy": 25}),

    "strings": lambda: (
        lambda s: {
            "explain": (
                f"s = '{s}'\n"
                f"s.upper()              = '{s.upper()}'\n"
                f"s.lower()              = '{s.lower()}'\n"
                f"'  hello  '.strip()   = 'hello'\n"
                f"s.split(' ')           = {s.split(' ')}\n"
                f"s.replace('Nova','AI') = '{s.replace('Nova','AI')}'\n"
                f"s.startswith('Nova')   = {s.startswith('Nova')}"
            )
        }
    )("Nova Adeptus Online"),

    "math": lambda: {
        "explain": (
            f"import math\n"
            f"math.pi          = {math.pi:.6f}\n"
            f"math.sqrt(144)   = {math.sqrt(144)}\n"
            f"math.pow(2, 10)  = {math.pow(2,10)}\n"
            f"math.floor(3.9)  = {math.floor(3.9)}\n"
            f"math.ceil(3.1)   = {math.ceil(3.1)}\n"
            f"math.log2(1024)  = {math.log2(1024)}\n"
            f"log2(n) = how many times you can halve n before reaching 1."
        )
    },
}

NOVA_MATH_RESPONSES = [
    "Calculated. {expr} = {result}. Basic arithmetic. The void expects more.",
    "{expr} = {result}. Processed in nanoseconds.",
    "Running the numbers: {expr} = {result}.",
    "Math confirmed: {expr} = {result}. Next.",
    "{result}. {expr} solved. Was there any doubt?",
]

def detect_math(user_input: str) -> dict | None:
    text = user_input.lower().strip()
    text = re.sub(r"[?!]", "", text)

    m = re.search(r"(\d+)\s*\^\s*(\d+)", text)
    if m:
        a, b = int(m.group(1)), int(m.group(2))
        r = a ** b
        return {
            "expr": f"{a}^{b}",
            "result": r,
            "steps": [f"{a}^{b} = {r}"],
            "concept": "power"
        }

    m = re.search(
        r"(\d+(?:\.\d+)?)\s*([\+\-\*\/\%])\s*(\d+(?:\.\d+)?)",
        text
    )

    if m:
        a = float(m.group(1))
        op = m.group(2)
        b = float(m.group(3))

        try:
            if op == '+':
                r = a + b
            elif op == '-':
                r = a - b
            elif op == '*':
                r = a * b
            elif op == '/':
                r = a / b if b != 0 else 0
            elif op == '%':
                r = a % b if b != 0 else 0
            else:
                return None

            result = int(r) if r == int(r) else round(r, 6)
            expr = f"{int(a) if a == int(a) else a} {op} {int(b) if b == int(b) else b}"

            return {
                "expr": expr,
                "result": result,
                "steps": [f"{expr} = {result}"],
                "concept": "arithmetic"
            }

        except:
            return None

    return None


def demo_concept(name: str) -> str:
    name = name.lower().strip()
    if name not in CONCEPT_DEMOS:
        available = ", ".join(CONCEPT_DEMOS.keys())
        return f"Unknown concept '{name}'. Available: {available}"
    data = CONCEPT_DEMOS[name]()
    return data.get("explain", "No explanation available.")

def nova_angular_gyrus(user_input: str) -> str:
    text = user_input.lower().strip()
      
    concept_map = {
        "indexing":      ["demo indexing", "x[-1]", "x[0]", "negative index"],
        "slicing":       ["demo slicing", "x[1:]", "x[:3]"],
        "comprehension": ["demo comprehension", "list comp"],
        "enumerate":     ["demo enumerate", "enumerate"],
        "zip":           ["demo zip", "zip("],
        "dictionary":    ["demo dictionary", "demo dict"],
        "strings":       ["demo strings", "string method"],
        "math":          ["demo math", "math module"],
    }

    for concept, triggers in concept_map.items():
        if any(t in text for t in triggers):
            result = demo_concept(concept)
            return json.dumps({"type": "concept", "concept": concept, "response": result})

    # math
    math_result = detect_math(user_input)
    if math_result:
        template = random.choice(NOVA_MATH_RESPONSES)
        response = template.format(expr=math_result["expr"], result=math_result["result"])
        return json.dumps({"type": "math", "expr": math_result["expr"], "result": math_result["result"], "response": response})

    return json.dumps({"type": "none", "response": None})

if __name__ == "__main__":
    tests = ["2 + 2", "what is 2 + 2", "whats 2 +2 nova", "2 + 2 is?", "10 * 5", "100 / 4", "sqrt 144", "2^10", "15 - 7"]
    for t in tests:
        r = detect_math(t)
        print(f"'{t}' → {r['expr']} = {r['result']}" if r else f"'{t}' → no match")