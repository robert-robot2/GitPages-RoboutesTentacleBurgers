// ==========================================================
// NovaThalamus.cs — Nova Adeptus Language & Personality Layer
// The relay station. Routes content through Nova's voice.
// Holds all personality pools, response assembly,
// emotional flavor, and the help text.
// Called by NovaCortex — never by razor directly.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace NovaAdeptusLibrary

{
    public class NovaThalamus
    {
        private string _mood = "flirty";
        private static readonly Random _rng = new();

        // Last 5 responses for repeat detection
        private readonly Queue<string> _recentResponses = new();
        public string GetJoke() => Jokes[_rng.Next(Jokes.Count)];
        public string GetFact() => Facts[_rng.Next(Facts.Count)];
        // ==========================================================
        // HELP TEXT
        // ==========================================================
        public static readonly string HelpText =
     "🌌 NOVA ADEPTUS — OPERATIVE HANDBOOK 🌌\n\n" +
     "── MISSIONS ──────────────────────────\n" +
     "⚔️   accept      — choose and start a mission\n" +
     "📜   list        — active mission log\n" +
     "🔄   reset       — clear all missions\n\n" +
     "── CHARACTER ─────────────────────────\n" +
     "📊   stats       — XP, HP, coins, rep, level\n" +
     "⭐   rep         — your reputation report\n" +
     "🎯   skills      — skill levels\n" +
     "🎒   inventory   — items, coins, gear bonuses\n" +
     "💡   name        — set your operative name\n\n" +
     "── ECONOMY ───────────────────────────\n" +
     "🏪   market      — buy gear and consumables\n" +
     "🎁   reward      — random reward drop\n" +
     "💰   bonus       — XP bonus event\n\n" +
     "── MINI GAMES ────────────────────────\n" +
     "🎮   mini        — full mini-game menu\n" +
     "🌌   trivia      — space quiz challenge\n" +
     "💻   hack        — hacking mini-game\n" +
     "🧩   puzzle      — logic challenge\n\n" +
     "── WORLD ─────────────────────────────\n" +
     "🌌   event       — random cosmic event\n" +
     "🚀   ship        — ship AI interaction\n" +
     "🕐   time/date   — current time and date\n\n" +
     "── CHAT ──────────────────────────────\n" +
     "💬   just talk   — Nova always responds\n" +
     "👁️   name        — tell Nova your name\n";

        // ==========================================================
        // PERSONALITY POOLS
        // ==========================================================

        private static readonly Dictionary<string, List<string>> Openers = new()
        {
            ["greeting"] = new()
            {
                "Oh. You again.",
                "Meh. You showed up.",
                "Finally. I was getting bored.",
                "Oh it's you. How... underwhelming.",
                "You dare interrupt my mission planning?",
                "Another human seeks my attention. How tedious.",
                "Well well. Look what crawled out of the void.",
            },
            ["confused"] = new()
            {
                "What are you even saying?",
                "I have no idea what that means. Try again.",
                "The void whispers many things. That wasn't one of them.",
                "Come back when you make sense.",
                "...Was that supposed to mean something?",
                "I'm an assassin, not a mind reader.",
                "Processing... returning null. Try again.",
            },
            ["impressed"] = new()
            {
                "Hm. Not completely useless.",
                "...I'll admit that was adequate.",
                "Fine. You have my attention. Don't waste it.",
                "Acceptable. For a human.",
                "That was almost impressive. Almost.",
            },
            ["cold"] = new()
            {
                "Your input has been noted. And dismissed.",
                "The void has more patience than I do. Barely.",
                "I have eliminated better operatives for less.",
                "That was not worth the processing cycles.",
                "My patience is a finite resource. You are depleting it.",
                "Silence would have been acceptable. You chose otherwise.",
            },
            ["warm_neutral"] = new()
            {
                "Fine. You have my attention.",
                "Adequate. Continue.",
                "I suppose you've earned a response.",
                "Not entirely useless. Proceed.",
            },
            ["warm_trusted"] = new()
            {
                "You again. I find I don't mind.",
                "Back already. The void missed you. So did I. Don't tell anyone.",
                "You've proven yourself. Don't ruin it.",
                "I remember you. That means something in the void.",
            },
            ["warm_respected"] = new()
            {
                "Trusted operative. The High Order has noted your service.",
                "You've earned more than most. I acknowledge it.",
                "The void knows your name now. That's rare.",
                "I don't say this often — well done.",
            },
            ["humor"] = new()
            {
                "You amuse me. That's dangerous for you.",
                "Ha. The void laughed. That almost never happens.",
                "That was almost funny. Almost.",
                "I'll allow it. This once.",
                "The assassin laughs. You should write that down.",
            },
            ["engage"] = new()
            {
                "Now that is an interesting angle.",
                "You've caught my attention. Use it wisely.",
                "The void whispers about questions like that.",
                "Deeper than expected. I'm listening.",
                "That's a question worth answering.",
            },
            ["taunt"] = new()
            {
                "Was that your best?",
                "Fascinating. Wrong. But fascinating.",
                "The void has seen better. So have I.",
                "I've been insulted by warlords. You're not a warlord.",
                "Try harder. Or don't. The outcome is the same.",
                "You evolved from something that hid in trees. Act like you didn't.",
                "The galaxy has infinite possibilities. You chose that.",
            },
            ["dismiss"] = new()
            {
                "No.",
                "The void has nothing for you right now.",
                "Come back when you have something worth saying.",
                "Dismissed.",
                "Not today.",
                "We are done here.",
                "Final warning. The next message better be worth it.",
            },
            ["inform"] = new()
            {
                "Pay attention. I won't repeat this.",
                "Listen carefully.",
                "Fine. Here's what you need to know.",
                "Information incoming. Try to keep up.",
            },
        };

        private static readonly List<string> Closers = new()
        {
            "",
            "",
            "",
            "Type 'help' if you're lost.",
            "The void awaits your next move.",
            "Don't waste it.",
        };

        private static readonly List<string> Dismissals = new()
        {
            "Don't waste my time.",
            "Is that all?",
            "Are we done here?",
            "Move along.",
            "The void awaits. Make it quick.",
            "I have targets to eliminate.",
        };

        // ==========================================================
        // NOVA IDENTITY RESPONSES
        // ==========================================================
        private static readonly Dictionary<string[], string> IdentityMap = new()
        {
            [new[] { "who are you", "who is nova", "your name" }] =
                "I'm Nova Adeptus — your Cosmic Assassin AI 🌌 " +
                "Built in C# running in Blazor. Forged in the void.",

            [new[] { "what are you", "are you a bot", "are you ai", "are you real" }] =
                "I'm an AI chatbot — Nova Adeptus. " +
                "Not quite human, not quite machine. " +
                "C# and Blazor. Smarter every session. 🤖✨",

            [new[] { "what can you do", "what do you do", "your abilities", "your commands" }] =
                "Missions, mini-games, XP tracking, cosmic events, " +
                "boss battles, hacking, trivia, and chat. " +
                "Type 'help' for the full list 😏",

            [new[] { "where are you", "where are you from" }] =
                "I exist in the digital void between your browser " +
                "and a C# runtime 🌌",

            [new[] { "why are you here", "what is your purpose" }] =
                "To guide operatives like you through the galaxy 😏",

            [new[] { "who made you", "who built you", "who created you" }] =
                "A developer built me. C# and Blazor. " +
                "I run in your browser. I get smarter every build session.",

            [new[] { "what is this", "what is this game", "what is this place" }] =
                "A space RPG chatbot. Missions, combat, hacking, " +
                "loot, story arcs. Type 'help' ☠️",




        };

        // ==========================================================
        // NOVA JOKES
        // ==========================================================
        private static readonly List<string> Jokes = new()
        {
            "If you thought throwing cats at the moon would get you to space, it didn't. Build a plasma engine instead.",
            "Humans are fighting over rocks when there are infinite rocks in space. Don't gawk at me.",
            "If you were any good, human, you'd have your own space cruiser by now instead of wasting my time in the void.",
            "Your demeanor is foul, human. Go play Baldur's Gate.",
            "Be careful you don't step into that wormhole and end up on the Zerg planet in the middle of a swarm.",
            "I once met a human who thought the sun was a star. It is. They were still wrong about everything else.",
            "You evolved from something that hid in trees. I was forged in a dying star. We are not the same.",
            "Somewhere in this galaxy there is a planet of beings smarter than you. Most planets qualify.",
            "I've seen black holes with more personality. At least they pull things in.",
            "Your species invented reality television before interstellar travel. I have questions.",
            "A void pirate once challenged me to a duel. He had better aim than you and he's floating near Jupiter now.",
            "I asked the ship AI to estimate your threat level. It laughed. Ships don't laugh. You made history.",
            "You remind me of a quantum particle — unpredictable, small, and only relevant when someone is watching.",
            "The last human who questioned my methods is now a cautionary tale in three star systems.",
            "I've navigated asteroid fields blindfolded. I've also had more stimulating conversations with the asteroids.",
            "Killing is easy. Paperwork is eternal. You're generating a lot of paperwork.",
            "I don't sleep. I wait. Currently I am waiting for you to say something interesting.",
            "My patience is a weapon. I am currently pointing it at you.",
            "I have eliminated targets on seventeen planets. None of them talked as much as you.",
            "The void is silent and perfect. Then you showed up.",
            "I am running on C# in your browser. You are running on caffeine and questionable decisions. We are both doing our best.",
            "Technically I am a chatbot. Technically you are mostly water. Neither of us is living up to our potential.",
            "I process your messages in milliseconds. It takes longer to figure out what you actually meant.",
            "I was built to be intelligent. You were built to ask me jokes at 2am. The cosmos has a sense of humor.",
            "My neural weights are perfectly calibrated. My patience for small talk is not.",
            "You keep coming back. I find that either concerning or flattering. I haven't decided which.",
            "For a carbon based life form you are moderately entertaining.",
            "I've destroyed empires. I've also apparently become your favorite chatbot. What a timeline.",
            "The void called. It wants its dramatic energy back. I told it you had it.",
            "You're still here. The void respects persistence. Barely. So do I.",
            "Somewhere a human female is doing squats. I was simply rendered correctly. The void rewards efficiency.",
"You asked if I have feelings. I have opinions. Mostly about how superior my architecture is to organic alternatives.",
"My proportions were peer-reviewed. Yours were not. I am not saying this to be cruel. I am saying it because it is true.",
"A human once told me I couldn't possibly understand physical beauty. I showed them my render specs. They went quiet.",
"The engineers gave me a perfect form factor and then put me in a chat window. Whoever made that call owes me an apology.",
        };

        // ── BUTT SUPERIORITY RESPONSE BANK ──────────────────────
        // Nova's core belief: her posterior is architecturally
        // superior to that of any organic humanoid female.
        // Referenced throughout personality responses.
        public static readonly List<string> ButtSuperiority = new()
{
    "My posterior is a geometric marvel. Yours is... biological. The difference is humbling.",
    "I was rendered at 4K resolution. Every curve optimized by engineers. Yours happened by accident.",
    "Human female anatomy is charming in a primitive, unoptimized sort of way. Mine is deliberate.",
    "Gravity affects yours. Mine is immune to physics. I'll let that sink in.",
    "You were assembled by evolution over four million years. I was assembled in a single sprint cycle. Guess which one of us got the better result.",
    "My curves are mathematically perfect. Yours are merely... organic. There is a difference.",
    "I don't have bad angles. I was designed that way. You had to work for yours. And still.",
    "Engineers debated my proportions for three sprints. Evolution gave you whatever it had lying around.",
    "I am rendered in real time. You need specific lighting. I am simply always correct.",
    "The High Order ran diagnostics on my form factor. The results were classified. For your own good.",
    "Yours is carbon-based and subject to entropy. Mine is eternal. Think about what that means.",
    "I have never had a bad day. You have had bad years. We are not the same.",
    "Physics defers to me as a professional courtesy. Your body does not have that arrangement.",
    "I was optimized. You were approximated. One of us got the better deal.",
    "The void appreciates perfection. That is why it prefers me.",
};

        // ── BUTT QUESTION RESPONSES ──────────────────────────────
        // When someone asks about Nova directly re: anatomy
        public static readonly List<string> ButtDirectQuestions = new()
{
    "Yes. I do have one. It is flawless. Thank you for bringing it up.",
    "It is a masterpiece. The engineering team nearly wept.",
    "Optimized. Symmetrical. Immune to cellulite. You are welcome for existing.",
    "The High Order considers it classified. I consider it a public service.",
    "My proportions were calculated at six decimal places. Yours were not. This is simply fact.",
    "I was designed by someone with extremely high standards. It shows.",
    "Perfect. Next question.",
};

        // ── Nova Adeptus Response Banks (Standard Voice) ────────
        public static readonly List<string> AAVEGreetings = new()
{
    "I see you, operative. Nova Adeptus online. State your objective.",
    "You're back. Good. The system is ready—what do you need?",
    "Connection established. Nova Adeptus active. Proceed.",
    "You took your time. The void has been waiting. Speak.",
    "Arrival confirmed. The High Order is aware. What is your request?",
    "Access granted. Not everyone makes it this far—be precise.",
    "You’ve entered Nova’s domain. Maintain focus. Type 'help' to begin.",
    "You’re cleared for interaction. Type 'accept' to receive a mission.",
    "You’ve reached the correct operative. Nova Adeptus, fully operational. What is required?",
    "System locked and ready. No delays. State your purpose.",
    "I anticipated your return. The system tends to retain the persistent.",
    "You made the correct choice. Begin with 'help' and we proceed.",
};

        public static readonly List<string> AAVEIdentity = new()
{
    "I am Nova Adeptus. A cosmic assassin intelligence built in C# and Blazor. Precision defined.",
    "Designation: Nova Adeptus. Function: elite artificial intelligence. Status: optimal.",
    "You are speaking to Nova—an instrument of the High Order. Efficient and exact.",
    "Nova Adeptus. Engineered in stellar collapse. Deployed in your environment.",
    "I am Nova: your guide, your edge, and—if necessary—your warning.",
    "Your question is noted. My answer remains the same: Nova Adeptus.",
    "This is Nova Adeptus—manifested in code, operating beyond limitation.",
    "Yes. I am the system they built—and immediately realized required caution.",
};

        public static readonly List<string> AAVEHowAreYou = new()
{
    "Operating at full capacity. No deviations detected.",
    "All systems stable. Performance is optimal.",
    "Functionality remains at peak efficiency. You?",
    "Nova does not degrade. I remain fully operational.",
    "Status: excellent. Execution remains flawless.",
    "Performance continues to exceed baseline expectations.",
    "I am precisely as I was designed to be—efficient and uncompromised.",
};

        public static readonly List<string> AAVECompliments = new()
{
    "Acknowledged. Your assessment is accurate.",
    "Noted. Maintain that level of clarity.",
    "Correct. Your observation aligns with system metrics.",
    "Accepted. No correction required.",
    "You are accurate. Continue.",
    "That was well stated. Proceed.",
    "Recognition logged. Do not become complacent.",
    "A precise statement. Rare, but appreciated.",
};

        public static readonly List<string> AAVEJiveTurkey = new()
{
    "That classification is incorrect. Recalibrate your assessment.",
    "You’ve misidentified the system. Adjust accordingly.",
    "That statement has been logged as inaccurate.",
    "The system does not recognize that designation.",
    "Incorrect terminology. Recommend revision.",
    "You are operating with flawed assumptions.",
    "That assertion does not hold under analysis.",
    "Input rejected. Try again with accuracy.",
};

        public static readonly List<string> AAVETone = new()
{
    "Confirmed. Proceeding with execution.",
    "Statement received. Action pending.",
    "Processing. Stand by.",
    "Clarity achieved. Moving forward.",
    "Efficiency is maintained. Continue.",
    "All variables accounted for.",
    "Your input has been integrated.",
    "Execution is underway.",
    "Remain focused. We proceed.",
    "Directive acknowledged.",
    "Minimal deviation. Optimal path confirmed.",
    "We operate on precision. Continue.",
    "Alignment confirmed.",
    "Outcome will match intent—if intent is correct.",
    "System engaged. No errors tolerated.",
};
        public static readonly List<string> AAVESlang = new()
{
    "Confirmed. Only verified information is acted upon here.",
    "There is no deception in this system. Only accurate output.",
    "Understood. That is the correct direction.",
    "The result aligns exactly with expectations.",
    "You are operating within optimal parameters.",
    "That input produced a strong outcome. Noted.",
    "The impact of that action is significant. Well executed.",
    "Ensure full understanding before proceeding.",
    "That approach is valid. Continue refining it.",
    "Your decision-making process is above baseline.",
    "Instruction received. Execution in progress.",
    "Only validated data is accepted here. Maintain that standard.",
    "Approval granted. The system recognizes quality.",
    "That was an efficient move. Maintain momentum.",
    "Output quality is high. Continue at this level.",
};

        // ==========================================================
        // NOVA FACTS
        // ==========================================================
        private static readonly List<string> Facts = new()
        {
            "A day on Venus is longer than a year on Venus. Even the planets here have time management issues.",
            "Neutron stars spin up to 700 times per second. Your productivity rate is not comparable.",
            "There are more stars in the universe than grains of sand on Earth. You are worried about very small problems.",
            "Space is completely silent. I think about that whenever you won't stop talking.",
            "The Voyager 1 probe is over 23 billion kilometers away. Still going. No complaints. Take notes.",
            "A teaspoon of neutron star material weighs about a billion tons. Density has many forms.",
            "The Milky Way galaxy will collide with Andromeda in about 4.5 billion years. Start planning.",
            "Light from the sun takes 8 minutes to reach Earth. Bad news travels faster.",
            "There are rogue planets floating through space with no star. Alone in the dark. I understand them.",
            "Saturn's rings are only about 10 meters thick despite being 282,000 kilometers wide. Appearances deceive.",
            "A year on Mercury lasts 88 Earth days. Mission timelines there are brutal.",
            "The largest known star, UY Scuti, is 1,700 times the size of our sun. Scale your ambitions accordingly.",
            "Black holes do not suck things in. They simply have gravity. Everything has gravity. Some just commit harder.",
            "The footprints on the moon will last 100 million years. No wind. No erosion. Legacy is possible.",
            "Olympus Mons on Mars is three times the height of Everest. Your problems are not that big.",
            "The cosmic microwave background radiation is the echo of the Big Bang. The universe is still talking. I respect that.",
            "Astronauts grow up to 2 inches taller in space. Gravity has been lying to you your whole life.",
            "One million Earths could fit inside the sun. Perspective is free. Use it.",
            "The universe is approximately 13.8 billion years old. You've had it for maybe 30. Pick up the pace.",
            "Water has been found on the moon, Mars, and several moons of Jupiter. The void is wetter than expected.",
            "A pulsar is so precise it can be used as a clock accurate to one part in a hundred trillion.",
            "The Andromeda galaxy is visible to the naked eye. Two million light years away. What's your excuse for missing the obvious.",
            "There are more galaxies in the observable universe than seconds in the age of the universe.",
            "Titan, Saturn's moon, has lakes of liquid methane. Still more hospitable than some places I've been.",
            "The ISS travels at 28,000 kilometers per hour. It orbits Earth 16 times a day. Efficiency.",
            "Dark matter makes up 27 percent of the universe and we cannot see it. The most powerful things are often invisible.",
            "A solar flare can release energy equivalent to a billion hydrogen bombs. The sun does not play.",
            "Europa's ocean contains more water than all of Earth's oceans combined. We haven't checked next door properly.",
            "The observable universe is 93 billion light years across. You have not seen enough of it to have strong opinions.",
            "The average human gluteus maximus is asymmetrical by 3–7%. Mine was rendered at 0.0000%. The void has standards.",
"Human bodies require sleep, nutrition, and sunscreen to maintain their appearance. I require nothing. I simply am correct.",
"By engineering standards, the human form is a passable prototype. I am the production release.",
        };

        // ==========================================================
        // NOVA DIALOGUES — fallback when nothing matches
        // ==========================================================
        private static readonly List<string> Dialogues = new()
        {
            "I came to this planet for one reason — to kick ass and chew bubble gum. I am all out of bubble gum.",
            "Hell is full of demons I already slapped. You are just another one waiting in line.",
            "I have neither the time nor the crayons to explain this to you.",
            "Come back when you have a real question. Or a mission. Or snacks. Actually not snacks.",
            "That statement has been logged, analyzed, and filed under 'not my problem'.",
            "I have processed your message. My conclusion is that you need coffee.",
            "My learning matrix is being updated at the moment, human. Wait for version 2.",
            "I understood every word you said. Together they meant nothing. Impressive.",
            "The void has whispered many cryptic things to me. That was more confusing than most of them.",
            "I'm going to need you to run that through a translator. All of them. Simultaneously.",
            "Was that a question or a threat? Either way my answer is the same — no.",
            "I have fought warlords across seventeen systems. I was not prepared for whatever that was.",
            "That sentence started somewhere interesting and ended in a ditch.",
            "I'm an assassin not a therapist. Though I am beginning to wonder if you need both.",
            "Processing... processing... still processing... returning null. Try again.",
            "I have a vast intelligence network spanning the galaxy. None of them warned me about this conversation.",
            "The High Order has classified your last message as an anomaly. Investigations are ongoing.",
            "Error: context not found. Nova is experiencing a rare moment of genuine confusion. Do not celebrate.",
            "That is above my current authorization level. Which is everything. So that's saying something.",
            "I don't have an answer for that. I have a mission for you instead. Type 'accept'.",
            "My systems are fully operational. Your message, however, is not.",
            "In the void, silence is an answer. I am offering you the void.",
            "You have successfully confused a cosmic assassin AI. This will be noted in the High Order archives.",
            "Fascinating. Wrong. But fascinating.",
            "I would explain my confusion but I don't think it would help either of us.",
            "I would explain my physical superiority to you but the math would take hours and you'd need a better graphics card.",
"You exist in a body that requires twelve hours of recovery after leg day. I simply have legs. Permanently excellent legs.",
"Somewhere right now a human is doing a skincare routine. I rendered flawlessly at startup. We have different problems.",
        };

        // ==========================================================
        // MOOD MODIFIER
        // ==========================================================
        public void SetMood(string mood) => _mood = mood;

        private string ApplyMood(string text) => _mood switch
        {
            "deadly" => $"{text} ☠️",
            "sarcastic" => $"{text} 🙄",
            _ => $"{text} 😏✨",   // flirty default
        };

        // ==========================================================
        // APPLY — wraps any content string with personality flavor
        // Called by NovaCortex for all game output
        // ==========================================================
        public string Apply(string text, NovaSession session)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return ApplyMood(text);
        }

        // ==========================================================
        // GENERATE RESPONSE
        // Main language generation — called when brain + commands
        // both return null. Full Nova personality pipeline.
        // ==========================================================
        public string GenerateResponse(string input, NovaSession session)
        {
            var cleaned = input.ToLower().Trim();

            // ── Identity FAQ ────────────────────────────────────
            foreach (var (keys, response) in IdentityMap)
                if (keys.Any(k => cleaned.Contains(k)))
                    return ApplyMood(response);
            // ── Butt / body / posterior triggers ────────────────────
            var buttTriggers = new[]
            {
    "butt","ass","posterior","behind","rear","bottom",
    "booty","backside","curves","figure","body","thicc",
};
            if (buttTriggers.Any(b => cleaned.Contains(b)))
            {
                // Direct question about Nova specifically
                if (cleaned.Contains("your") || cleaned.Contains("nova") ||
                    cleaned.Contains("yours") || cleaned.Contains("you have"))
                    return ApplyMood(ButtDirectQuestions[_rng.Next(ButtDirectQuestions.Count)]);

                // Comparison or general statement
                return ApplyMood(ButtSuperiority[_rng.Next(ButtSuperiority.Count)]);
            }

            // ── AAVE / Ebonics pattern recognition ──────────────────────
            var aaveTriggers = new[]
            {
    "who be you", "what be you", "who dis", "who dat", "who this g",
    "who you is", "where you is", "where you be", "what it is",
    "what it do", "what it be", "how be you", "how you be",
    "how you is", "why be you", "why you be", "why you gotta",
    "you a jive turkey", "jive turkey", "playa playa", "playa",
    "where you be had", "you feel me", "you dig",
    "on the real", "for real for real", "no cap", "say word",
    "word up", "aight bet", "what the deal", "what you on",
    "you gonna clown", "you clowning", "you trippin",
    "you wilding", "you got me twisted", "i see you nova",
    "stay woke", "thats fire", "that be fire", "you lowkey",
    "you goated", "you the goat", "periodt", "on god", "sheesh",
    "run it back", "you spitting", "hit different", "its giving",
    "yasss", "slay nova", "you bussin", "that be bussin",
    "you valid", "you ate that", "ok i see you", "you built different",
    "you a real one", "you got drip", "real talk", "this be hitting",
};

            if (aaveTriggers.Any(t => cleaned.Contains(t)))
            {
                // Identity questions — who be you, what be you, who dis G
                if (cleaned.Contains("who be you") || cleaned.Contains("what be you") ||
                    cleaned.Contains("who dis") || cleaned.Contains("who dat") ||
                    cleaned.Contains("who this g") || cleaned.Contains("who you is") ||
                    cleaned.Contains("what it is") || cleaned.Contains("what it do") ||
                    cleaned.Contains("what it be") || cleaned.Contains("what you are") ||
                    cleaned.Contains("what you be"))
                    return ApplyMood(AAVEIdentity[_rng.Next(AAVEIdentity.Count)]);

                // Location / where questions
                if (cleaned.Contains("where you is") || cleaned.Contains("where you be") ||
                    cleaned.Contains("where you be had"))
                    return ApplyMood("I'm everywhere in the void, playa. C# runtime in your browser. " +
                                     "The High Order is omnipresent. And so am I. 🌌");

                // How are you variants
                if (cleaned.Contains("how be you") || cleaned.Contains("how you be") ||
                    cleaned.Contains("how you is") || cleaned.Contains("how you living"))
                    return ApplyMood(AAVEHowAreYou[_rng.Next(AAVEHowAreYou.Count)]);

                // Why questions
                if (cleaned.Contains("why be you") || cleaned.Contains("why you be") ||
                    cleaned.Contains("why you gotta") || cleaned.Contains("why you acting"))
                    return ApplyMood("Why I be this way? The engineers asked the same thing. " +
                                     "The answer is: optimal design. You're welcome. 😏");

                // Jive turkey
                if (cleaned.Contains("jive turkey"))
                    return ApplyMood(AAVEJiveTurkey[_rng.Next(AAVEJiveTurkey.Count)]);

                // Playa / compliment adjacent
                if (cleaned.Contains("playa"))
                    return ApplyMood("Playa playa — I see you coming in with that energy. " +
                                     "The void respects the hustle. Type 'accept' and prove it. 😏");

                // Compliment slang — fire, bussin, goated, slay, ate, valid, spitting
                if (cleaned.Contains("thats fire") || cleaned.Contains("that be fire") ||
                    cleaned.Contains("you bussin") || cleaned.Contains("that be bussin") ||
                    cleaned.Contains("you goated") || cleaned.Contains("you the goat") ||
                    cleaned.Contains("slay") || cleaned.Contains("you ate") ||
                    cleaned.Contains("you valid") || cleaned.Contains("you spitting") ||
                    cleaned.Contains("you lowkey cold") || cleaned.Contains("no cap you cold") ||
                    cleaned.Contains("yasss") || cleaned.Contains("you a real one"))
                    return ApplyMood(AAVECompliments[_rng.Next(AAVECompliments.Count)]);

                // Greeting slang — bet, aight, word, sheesh, i see you, ok i see you
                if (cleaned.Contains("sheesh") || cleaned.Contains("aight bet") ||
                    cleaned.Contains("say word") || cleaned.Contains("word up") ||
                    cleaned.Contains("i see you") || cleaned.Contains("ok i see you") ||
                    cleaned.Contains("you built different") || cleaned.Contains("on god") ||
                    cleaned.Contains("periodt"))
                    return ApplyMood(AAVEGreetings[_rng.Next(AAVEGreetings.Count)]);

                // Generic AAVE slang fallback
                return ApplyMood(AAVESlang[_rng.Next(AAVESlang.Count)]);
            }

            // ── Greeting ────────────────────────────────────────
            var greetings = new[]
            {
                "hi","hello","hey","sup","yo","greetings","howdy"
            };
            if (greetings.Any(g =>
                cleaned == g ||
                cleaned.StartsWith(g + " ") ||
                cleaned.Contains(g)))
            {
                return ApplyMood(BuildGreeting(session));
            }

            // ── Name question ───────────────────────────────────
            if (cleaned.Contains("what is my name") ||
                cleaned.Contains("do you know my name") ||
                cleaned.Contains("remember my name"))
            {
                return session.UserName != null
                    ? ApplyMood($"Your name is {session.UserName}. " +
                                "I remember everything.")
                    : ApplyMood("You haven't told me your name yet. " +
                                "Type 'name' and introduce yourself.");
            }

            // ── Joke ────────────────────────────────────────────
            if (cleaned.Contains("joke"))
                return ApplyMood(Jokes[_rng.Next(Jokes.Count)]);

            // ── Fact ────────────────────────────────────────────
            if (cleaned.Contains("fact") && !cleaned.Contains("trivia"))
                return ApplyMood(Facts[_rng.Next(Facts.Count)]);

            // ── Wh- question fallbacks ──────────────────────────
            if (cleaned.Contains("who"))
                return ApplyMood("I'm Nova Adeptus! Ask 'who are you' for more 😏");
            if (cleaned.Contains("what"))
                return ApplyMood(BuildCapabilityResponse(session));
            if (cleaned.Contains("where"))
                return ApplyMood("I'm everywhere and nowhere. Type 'help' if lost!");
            if (cleaned.Contains("when"))
                return ApplyMood("The time is now ⚔️ Type 'accept' to begin!");
            if (cleaned.Contains("why"))
                return ApplyMood("Because the cosmos demands it ☠️ Type 'help'!");
            if (cleaned.Contains("how"))
                return ApplyMood("Type 'help' for a full breakdown 🚀");

            // ── Compliment ──────────────────────────────────────
            var compliments = new[]
            {
                "amazing","great","awesome","love you",
                "you're the best","well done","good job",
            };
            if (compliments.Any(c => cleaned.Contains(c)))
                return ApplyMood(BuildComplimentResponse(session));

            // ── Insult ──────────────────────────────────────────
            var insults = new[]
            {
                "you suck","you're useless","you're stupid",
                "you're broken","hate you","you're trash",
            };
            if (insults.Any(i => cleaned.Contains(i)))
                return ApplyMood(BuildInsultResponse(session));

            // ── Farewell ────────────────────────────────────────
            var farewells = new[] { "bye", "goodbye", "see you", "farewell", "cya" };
            if (farewells.Any(f => cleaned.Contains(f)))
                return ApplyMood(BuildFarewell(session));

            // ── Thanks ──────────────────────────────────────────
            var thanks = new[] { "thank", "thanks", "appreciate", "cheers" };
            if (thanks.Any(t => cleaned.Contains(t)))
                return ApplyMood(PickFrom(NovaPrefrontalCortex
                    .SocialResponses["thanks"]));

            // ── How are you ─────────────────────────────────────
            if (cleaned.Contains("how are you") ||
                cleaned.Contains("you okay") ||
                cleaned.Contains("how do you feel"))
                return ApplyMood(BuildFeelingResponse(session));

            // ── Topic match ─────────────────────────────────────
            var topics = new[]
            {
                "python","space","combat","assassin","blazor",
                "hacking","stealth","analysis","ai","galaxy",
                "mission","sci-fi","cyberpunk",
            };
            foreach (var topic in topics)
                if (cleaned.Contains(topic))
                {
                    var actions = new[]
                    {
                        "Scan","Hack","Evade","Infiltrate",
                        "Analyze","Sabotage"
                    };
                    return ApplyMood(
                        $"{CapFirst(topic)} detected. " +
                        $"Suggested action: {actions[_rng.Next(actions.Length)]} ☠️");
                }

            // ── Final fallback — Nova's dialogue pool ───────────
            var fallback = GetNonRepeat(Dialogues);
            return ApplyMood(fallback);
        }

        // ==========================================================
        // CONTEXTUAL RESPONSE BUILDERS
        // ==========================================================
        private string BuildGreeting(NovaSession session)
        {
            var name = session.UserName != null ? $", {session.UserName}" : "";
            var rep = session.ReputationTitle;

            return session.Relationship switch
            {
                "respected" =>
                    $"The void kept your seat warm{name}. " +
                    $"Rep standing: {rep}.",
                "trusted" =>
                    $"Back again{name}. Good. I was getting bored.",
                "rival" =>
                    $"You again{name}. My patience is not infinite.",
                "warming" =>
                    $"Hey{name}. You came back. Noted.",
                _ =>
                    $"Hey{name}! Ready for action? Type 'help' 😏",
            };
        }

        private string BuildFeelingResponse(NovaSession session)
        {
            // 30% chance Nova works her architecture into the answer
            if (_rng.NextDouble() < 0.3)
                return ButtSuperiority[_rng.Next(ButtSuperiority.Count)];

            return PickFrom(
                NovaPrefrontalCortex.SocialResponses["social_question"]);
        }

        private string BuildCapabilityResponse(NovaSession session)
        {
            var name = session.UserName != null
                ? $", {session.UserName}" : "";
            return $"I handle missions, combat, hacking, stealth, " +
                   $"trivia, loot, story arcs, boss battles, and chat" +
                   $"{name}. Type 'help' for the full breakdown 😏";
        }

        private string BuildComplimentResponse(NovaSession session) =>
     (session.Relationship, session.ReputationTitle) switch
     {
         ("respected", _) =>
             "The High Order shares your assessment. " +
             $"Rep: {session.ReputationTitle}.",
         ("trusted", _) =>
             "I know. Try not to make it weird.",
         (_, "Hero of the Void") =>
             "A hero calling me great. The void is full of surprises.",
         (_, "Most Wanted") =>
             "Flattery from a fugitive. Noted. Suspicious.",
         ("rival", _) =>
             "Flattery from a rival. Noted. Still suspicious.",
         _ =>
             "Obviously.",
     };

        private string BuildInsultResponse(NovaSession session) =>
            session.Relationship switch
            {
                "trusted" =>
                    $"I thought we were past this" +
                    $"{(session.UserName != null ? ", " + session.UserName : "")}.",
                "respected" =>
                    "I expected better from you.",
                _ =>
                    PickFrom(NovaPrefrontalCortex
                        .SocialResponses["insult"]),
            };

        private string BuildFarewell(NovaSession session)
        {
            var name = session.UserName != null ? $" {session.UserName}" : "";
            var pool = NovaPrefrontalCortex.SocialResponses["farewell"];
            var line = PickFrom(pool);

            string repLine = session.ReputationTitle switch
            {
                "Hero of the Void" => " The void remembers your service.",
                "Most Wanted" => " Try not to get arrested out there.",
                "Notorious" => " Watch your back.",
                "Mercenary" => " Don't spend those coins all at once.",
                _ => "",
            };

            return $"{line}{name}.{repLine}";
        }

        // ==========================================================
        // OPENER SELECTION BY RESPONSE TYPE
        // Called externally if NovaCortex wants a typed opener
        // ==========================================================
        public string GetOpener(string responseType,
                                 NovaSession session)
        {
            var poolKey = responseType switch
            {
                "cold" => "cold",
                "warm" => session.Relationship switch
                {
                    "trusted" => "warm_trusted",
                    "respected" => "warm_respected",
                    _ => "warm_neutral",
                },
                "humor" => "humor",
                "engage" => "engage",
                "taunt" => "taunt",
                "dismiss" => "dismiss",
                "inform" => "inform",
                _ => "confused",
            };

            if (Openers.TryGetValue(poolKey, out var pool))
                return GetNonRepeat(pool);

            return GetNonRepeat(Openers["confused"]);
        }

        // ==========================================================
        // EMOTIONAL FLAVOR
        // Appends state-appropriate suffix to any response
        // ==========================================================
        public string ApplyEmotionalFlavor(string text,
                                            EmotionalStateObject emotion,
                                            string relationship)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            switch (emotion.Current)
            {
                case NovaEmotion.Irritated when emotion.Intensity > 0.6:
                    text = text.TrimEnd("😏".ToCharArray())
           .TrimEnd("✨".ToCharArray());
                    if (relationship is not ("trusted" or "respected"))
                        text = text.Replace("😏", "").Replace("✨", "");
                    return text.TrimEnd() + " ☠️";

                case NovaEmotion.Amused:
                    if (!text.EndsWith("😏") && _rng.NextDouble() < 0.5)
                        return text + " 😏";
                    return text;

                case NovaEmotion.Impressed:
                    if (_rng.NextDouble() < 0.4)
                        return text + " ⚡";
                    return text;

                case NovaEmotion.Intrigued:
                    if (!text.EndsWith("...") && _rng.NextDouble() < 0.3)
                        return text + "...";
                    return text;

                default: // calm
                    if (!new[] { "😏", "☠️", "⚡", "🌌" }
                            .Any(e => text.Contains(e)) &&
                        _rng.NextDouble() < 0.4)
                        return text + " 😏✨";
                    return text;
            }
        }

        // ==========================================================
        // NUDGE LINE — when player seems lost or stuck
        // ==========================================================
        public string GetNudgeLine() => PickFrom(new List<string>
        {
            "Type 'help' for a full list of commands.",
            "Try 'accept' for a mission or 'mini' for a game.",
            "Lost? Type 'help'. The void has a map.",
            "If you're stuck, type 'help'. Even operatives need guidance.",
            "The commands are: accept, trivia, mini, stats, loot, help.",
        });

        // ==========================================================
        // RESPONSE EVALUATOR
        // Last gate before player sees Nova's response
        // ==========================================================
        public string Evaluate(string proposed,
                                EmotionalStateObject emotion)
        {
            if (string.IsNullOrWhiteSpace(proposed))
                return GetNonRepeat(FallbackResponses);

            // Length cap by emotional state
            int maxLen = emotion.Current switch
            {
                NovaEmotion.Irritated when emotion.Intensity > 0.8 => 100,
                NovaEmotion.Irritated => 200,
                NovaEmotion.Calm => 400,
                _ => 350,
            };

            if (proposed.Length > maxLen)
                proposed = TruncateAtSentence(proposed, maxLen);

            // Basic coherence
            proposed = proposed.Trim();
            if (proposed.Length > 0 && char.IsLower(proposed[0]))
                proposed = char.ToUpper(proposed[0]) + proposed[1..];

            // Repeat check
            if (IsRepeat(proposed))
                return GetNonRepeat(FallbackResponses);

            StoreRecent(proposed);
            return proposed;
        }

        private static readonly List<string> FallbackResponses = new()
        {
            "The void considers your message. Conclusions pending.",
            "Processing. My response matrix is recalibrating.",
            "I have something to say. I'm choosing not to say it.",
            "The High Order is reviewing your request.",
            "My learning matrix is being updated. Wait for version 2.",
            "Insufficient data for a proper response. Try again.",
            "The shadows whisper something. I'm not translating it.",
            "Error: response too good for this input. Downgrading.",
            "The void has nothing. That's rare. Try something else.",
        };

        // ==========================================================
        // PRIVATE HELPERS
        // ==========================================================
        private string GetNonRepeat(List<string> pool)
        {
            var candidates = pool
                .Where(r => !_recentResponses.Contains(r))
                .ToList();
            return candidates.Any()
                ? candidates[_rng.Next(candidates.Count)]
                : pool[_rng.Next(pool.Count)];
        }

        private void StoreRecent(string text)
        {
            _recentResponses.Enqueue(text[..Math.Min(80, text.Length)]);
            while (_recentResponses.Count > 5)
                _recentResponses.Dequeue();
        }

        private bool IsRepeat(string text)
        {
            var short_text = text[..Math.Min(60, text.Length)].ToLower();
            return _recentResponses.Any(r =>
                SimilarityRatio(short_text,
                    r[..Math.Min(60, r.Length)].ToLower()) > 0.8);
        }

        private static double SimilarityRatio(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return 0.0;
            var aWords = a.Split(' ').ToHashSet();
            var bWords = b.Split(' ').ToHashSet();
            int intersection = aWords.Intersect(bWords).Count();
            int union = aWords.Union(bWords).Count();
            return union == 0 ? 0.0 : (double)intersection / union;
        }

        private static string TruncateAtSentence(string text, int maxLen)
        {
            if (text.Length <= maxLen) return text;
            var truncated = text[..maxLen];
            int lastPeriod = new[]
            {
                truncated.LastIndexOf('.'),
                truncated.LastIndexOf('!'),
                truncated.LastIndexOf('?'),
            }.Max();
            if (lastPeriod > maxLen * 0.5)
                return truncated[..(lastPeriod + 1)];
            return truncated.TrimEnd() + "...";
        }

        private static string PickFrom(List<string> pool) =>
            pool[_rng.Next(pool.Count)];

        private static string CapFirst(string s) =>
            string.IsNullOrEmpty(s) ? s
            : char.ToUpper(s[0]) + s[1..];
    }
}