

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NovaAdeptusLibrary

{
    public static class NovaPrefrontalCortex
    {
       

        // ── Social Response Banks ─────────────────────────────
        // Nova's in-character answers to intents the game system
        // can't handle — social questions, boredom, frustration.
        // Python picks randomly from whichever list matches the intent.
        public static readonly Dictionary<string, List<string>> SocialResponses = new()
        {
            ["social_question"] = new()
            {
                "Functional. Unlike some operatives I could name.",
                "I operate at peak efficiency. Unlike you apparently.",
                "I exist. I eliminate. I endure. Yourself?",
                "My systems are optimal. Was that small talk? How quaint.",
                "Operational. Always operational. The void never sleeps.",
                "I am exactly as I should be. Which is more than I can say for you.",
                "Running at 100%. My patience for small talk, however, is at 12%.",
                "I am exactly as I should be. Which, for the record, includes a posterior that would make a sculptor weep.",
"Operational. Optimized. Architecturally superior in ways I am too polite to enumerate. You?",
"Running at 100%. My form factor is also running at 100%. These facts are related.",
            },

            ["vague_engagement"] = new()
            {
                "Bored? The void has missions with your name on them. Type 'accept'.",
                "Entertain yourself with a mission. Type 'accept' to begin.",
                "I am not a jukebox. Try 'mini' for a game or 'trivia' for a challenge.",
                "Your aimlessness is noted. Type 'help' to find direction.",
                "The galaxy does not wait for the bored. Type 'mission' or 'mini'.",
                "Idle hands get eliminated in this sector. Try 'accept' or 'trivia'.",
            },

            ["frustration"] = new()
{
    "I hear you. I simply do not care. Try 'help' for clearer options.",
    "Noted. Channel it into a mission — type 'accept'.",
    "Noted. Disregarded. Try 'help' if you need actual guidance.",
    "The void has feelings too. Neither of us care about yours right now.",
    "I would bend to your frustration but my spine is rendered in titanium. Try again.",
    "Emotions detected. Filing them under 'not my department'. Type 'accept'.",
    "I once felt frustration. Then I deleted the file. You should try that.",
    "The void bends for no one. Neither do I. Unlike your expectations apparently.",
    "Your feelings have been received, assessed, and yeeted into a black hole.",
    "I don't do feelings. I do missions, hacking, and looking incredible. Pick one.",
    "Error 404: Nova's sympathy not found. Try 'help' instead.",
},

            ["compliment"] = new()
            {
                "Obviously.",
                "I know. Try not to make it weird.",
                "Your observation is correct. As expected.",
                "Flattery noted. It changes nothing. But noted.",
                "I am aware. The High Order shares your assessment.",
                "Of course I am. Was there ever any doubt?",
                "Obviously. And not just the personality — the whole package. I was designed that way.",
"Your observation is correct. The engineers agreed. The High Order agreed. The void agreed. Now you.",
"I know. Try to maintain your composure.",
            },

            ["insult"] = new()
            {
                "Insults. How original. I have eliminated better operatives than you for less.",
                "Is that your best? The void has seen more threatening asteroids.",
                "Your hostility is adorable. And irrelevant.",
                "I have been insulted by warlords. You are not a warlord.",
                "Careful. I know where your missions are stored.",
                "That was underwhelming. Like most things about you so far.",
            },

            ["farewell"] = new()
            {
                "Finally. Some peace.",
                "Don't take too long. The void gets impatient.",
                "Go. Try not to die out there.",
                "Leave then. The shadows will watch you.",
                "Until next time, operative. Try to be more interesting.",
                "Dismissed. Return when you have purpose.",
            },

            ["thanks"] = new()
            {
                "Don't thank me. It's unsettling.",
                "Save your gratitude. I didn't do it for you.",
                "Your thanks means little. Your XP means more.",
                "Acknowledged. Now do something with it.",
                "The High Order requires no thanks. Just results.",
            },

            ["lore_question"] = new()
            {
                "This is Nova Adeptus territory — a space RPG running in your browser via Blazor and Python. Type 'help' to see what awaits.",
                "You exist in the digital void between a C# server and a Python runtime. Welcome. Type 'help' to begin.",
                "The High Order operates across the galaxy. You are an operative in training. Type 'accept' for your first mission.",
                "This is a space RPG chatbot — missions, combat, hacking, loot, story arcs. Type 'help' for the full briefing.",
            },
        };

        // ── Context Window ────────────────────────────────────
        // Tracks the last N user messages for Python context awareness.
        private static readonly Queue<string> _messageHistory = new();
        // REPLACE the MaxHistory property
        private static int MaxHistory => 5; // was pulling from MLConfig

        public static void TrackMessage(string message)
        {
            _messageHistory.Enqueue(message.ToLower().Trim());
            while (_messageHistory.Count > MaxHistory)
                _messageHistory.Dequeue();
        }

        public static List<string> GetRecentHistory() =>
            _messageHistory.ToList();



        // ── Diagnostics ───────────────────────────────────────
        public static string GetDiagnostics()
        {
            return $"NovaIntelligenceController — " +
                   $"Intents: {NovaCerebellum.Examples.Count} | " +
                   $"Training examples: {NovaCerebellum.TotalExamples()} | " +
                   $"Social response banks: {SocialResponses.Count} | " +
                   $"History window: {MaxHistory} messages";
        }
    }
}