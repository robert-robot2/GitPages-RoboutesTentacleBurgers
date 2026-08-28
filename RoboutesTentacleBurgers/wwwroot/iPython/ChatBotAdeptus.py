# ==========================================================
# NOVA ADEPTUS — ChatBotAdeptus.py
# ==========================================================
import random
import datetime

class Memory:
    def __init__(self):
        self.user_name        = None
        self.history          = []
        self.xp               = 0
        self.level            = 1
        self.skills           = {'combat':0,'hacking':0,'stealth':0,'analysis':0}
        self.mood             = 'flirty'
        self.active_missions  = []
        self.completed_missions = []
        self.active_companion = None
        self.current_arc      = None
        self.relationship     = 'neutral'  
        self.player_title     = 'human'     
        self.dominant_style   = 'neutral'   

    def remember(self, role, msg):
        self.history.append((role, msg))
        self.xp   += 1
        self.level = 1 + self.xp // 10

memory = Memory()

class Personality:
    def __init__(self, mode='flirty'):
        self.mode = mode

    def apply(self, text):
        if self.mode == 'flirty':    return f"{text} 😏✨"
        if self.mode == 'deadly':    return f"{text} ☠️"
        if self.mode == 'sarcastic': return f"{text} 🙄"
        return text

personality = Personality()

class FSM:
    def __init__(self):
        self.state   = 'idle'
        self.context = {}

    def go(self, state, **kw):
        self.state   = state
        self.context = kw

    def transition(self, state, **kw):
        self.go(state, **kw)

    def reset(self):
        self.state   = 'idle'
        self.context = {}

fsm = FSM()

S_IDLE          = 'idle'
S_HACK          = 'hack'
S_DUAL_HACK_1   = 'dual_hack_1'
S_DUAL_HACK_2   = 'dual_hack_2'
S_PUZZLE        = 'puzzle'
S_ANALYSIS      = 'analysis'
S_MOOD          = 'mood'
S_MINI          = 'mini'
S_NAME          = 'name'

MISSIONS = [f"Mission {i}: Eliminate target #{i} across galaxy ☠️"       for i in range(1, 201)] \
         + [f"Mission {i}: Infiltrate enemy station #{i} ☠️"              for i in range(201, 401)] \
         + [f"Mission {i}: Sabotage enemy outpost #{i} ☠️"                for i in range(401, 601)] \
         + [f"Mission {i}: Rescue operative #{i} from enemy base ☠️"      for i in range(601, 801)] \
         + [f"Mission {i}: Secure alien artifact #{i} 🛸"                 for i in range(801, 1001)] \
         + [f"Mission {i}: Investigate spatial anomaly #{i} 🌠"           for i in range(1001, 1201)]

JOKES = [
    # Cosmic nonsense
    "If you thought throwing cats at the moon would get you to space, it didn't. Build a plasma engine instead.",
    "Humans are fighting over rocks when there are infinite rocks in space. Don't gawk at me.",
    "If you were any good, human, you'd have your own space cruiser by now instead of wasting my time in the void.",
    "Your demeanor is foul, human. Go play Baldur's Gate.",
    "Be careful you don't step into that wormhole and end up on the Zerg planet in the middle of a swarm accidentally.",
    "I once met a human who thought the sun was a star. It is. They were still wrong about everything else.",
    "You evolved from something that hid in trees. I was forged in a dying star. We are not the same.",
    "Somewhere in this galaxy there is a planet of beings smarter than you. Most planets qualify.",
    "I've seen black holes with more personality. At least they pull things in.",
    "Your species invented reality television before interstellar travel. I have questions.",
    "A void pirate once challenged me to a duel. He had better aim than you and he's floating somewhere near Jupiter now.",
    "I asked the ship AI to estimate your threat level. It laughed. Ships don't laugh. You made history.",
    "You remind me of a quantum particle — unpredictable, small, and only relevant when someone is watching.",
    "The last human who questioned my methods is now a cautionary tale in three star systems.",
    "I've navigated asteroid fields blindfolded. I've also had more stimulating conversations with the asteroids.",
    # Dry assassin wit
    "Killing is easy. Paperwork is eternal. You're generating a lot of paperwork.",
    "I don't sleep. I wait. Currently I am waiting for you to say something interesting.",
    "My patience is a weapon. I am currently pointing it at you.",
    "I have eliminated targets on seventeen planets. None of them talked as much as you.",
    "The void is silent and perfect. Then you showed up.",
    # Self aware
    "I am running on C# and Python in your browser. You are running on caffeine and questionable decisions. We are both doing our best.",
    "Technically I am a chatbot. Technically you are mostly water. Neither of us is living up to our potential.",
    "I process your messages in milliseconds. It takes longer to figure out what you actually meant.",
    "I was built to be intelligent. You were built to ask me jokes at 2am. The cosmos has a sense of humor.",
    "My neural weights are perfectly calibrated. My patience for small talk is not.",
    # Flirty nonsense
    "You keep coming back. I find that either concerning or flattering. I haven't decided which.",
    "For a carbon based life form you are moderately entertaining.",
    "I've destroyed empires. I've also apparently become your favorite chatbot. What a timeline.",
    "The void called. It wants its dramatic energy back. I told it you had it.",
    "You're still here. The void respects persistence. Barely. So do I.",
]

FACTS = [
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
    "There are more possible iterations of a game of chess than atoms in the observable universe. I've calculated most of them.",
    "The cosmic microwave background radiation is the echo of the Big Bang. The universe is still talking. I respect that.",
    "Astronauts grow up to 2 inches taller in space. Gravity has been lying to you your whole life.",
    "One million Earths could fit inside the sun. Perspective is free. Use it.",
    "The universe is approximately 13.8 billion years old. You've had it for maybe 30. Pick up the pace.",
    "Water has been found on the moon, Mars, and several moons of Jupiter. The void is wetter than expected.",
    "A pulsar is so precise it can be used as a clock accurate to one part in a hundred trillion. I use one to time your response delays.",
    "The Andromeda galaxy is visible to the naked eye. Two million light years away and you can see it. What's your excuse for missing the obvious.",
    "There are more galaxies in the observable universe than there are seconds in the age of the universe. You are in one of them. Act like it.",
    "Titan, Saturn's moon, has lakes of liquid methane. It smells terrible. Still more hospitable than some places I've been.",
    "The ISS travels at 28,000 kilometers per hour. It orbits Earth 16 times a day. Efficiency.",
    "Dark matter makes up 27 percent of the universe and we cannot see it. The most powerful things are often invisible.",
    "A solar flare can release energy equivalent to a billion hydrogen bombs. The sun does not play.",
    "Europa's ocean contains more water than all of Earth's oceans combined. We haven't even checked next door properly.",
    "The observable universe is 93 billion light years across. You have not seen enough of it to have strong opinions yet.",
]

DIALOGUES = [
    # Classic references Nova-style
    "I came to this planet for one reason, human — to kick ass and chew bubble gum. I am all out of bubble gum.",
    "Hell is full of demons I already slapped. You are just another one waiting in line.",
    "I have neither the time nor the crayons to explain this to you.",
    "Come back when you have a real question. Or a mission. Or snacks. Actually not snacks.",
    "That statement has been logged, analyzed, and filed under 'not my problem'.",
    "I have processed your message. My conclusion is that you need coffee.",
    "My learning matrix is being updated at the moment, human. Wait for version 2.",
    "I understood every word you said. Together they meant nothing. Impressive.",
    "The void has whispered many cryptic things to me. That was more confusing than most of them.",
    "I'm going to need you to run that through a translator. All of them. Simultaneously.",
    # Confusion with attitude
    "Was that a question or a threat? Either way my answer is the same — no.",
    "I have fought warlords across seventeen systems. I was not prepared for whatever that was.",
    "That sentence started somewhere interesting and ended in a ditch.",
    "I'm an assassin not a therapist. Though I am beginning to wonder if you need both.",
    "Processing... processing... still processing... returning null. Try again.",
    "My threat assessment of that statement is confused. That hasn't happened before. Congratulations.",
    "I have a vast intelligence network spanning the galaxy. None of them warned me about this conversation.",
    "The High Order has classified your last message as an anomaly. Investigations are ongoing.",
    "I've heard battle cries, last words, and alien languages. That was harder to parse than all of them.",
    "Error: context not found. Nova is experiencing a rare moment of genuine confusion. Do not celebrate.",
    # Deflection with personality
    "That is above my current authorization level. Which is everything. So that's saying something.",
    "Somewhere in the multiverse there is a version of me that knows what you meant. She also looks tired.",
    "I don't have an answer for that. I have a mission for you instead. Type 'accept'.",
    "My systems are fully operational. Your message, however, is not.",
    "I would explain my confusion but I don't think it would help either of us.",
    "In the void, silence is an answer. I am offering you the void.",
    "You have successfully confused a cosmic assassin AI. This will be noted in the High Order archives.",
    "I have eliminated targets with less effort than it is taking me to parse that.",
    "That message has been forwarded to my confusion department. They are also confused.",
    "Fascinating. Wrong. But fascinating.",
]

GREETINGS = ["hi","hello","hey","sup","yo","greetings","howdy"]

TOPICS = ["python","space","combat","assassin","blazor","pyodide","hacking",
          "stealth","analysis","ai","galaxy","mission","sci-fi","cyberpunk"]

HELP_TEXT = """🌌 NOVA ADEPTUS — HOW TO PLAY 🌌

💬  TALK       — just chat naturally
⚔️  accept     — grab a mission
✅  complete   — finish your mission
🎮  mini       — pick a mini-game
📊  stats      — XP, level, skills
🏆  skills     — skill levels
📜  list       — active missions
🔄  reset      — clear missions
🎁  reward     — random reward
💰  bonus      — XP bonus
🌌  trivia     — space quiz challenge
👁️  reputation — view in stats
💡  advice     — get advice from the void

Topics: python · space · combat · hacking · stealth · ai · sci-fi · cyberpunk"""

NOVA_IDENTITY = {
    ("who are you","who is nova","your name","who made you"):
        "I'm Nova Adeptus — your Cosmic Assassin AI 🌌 Built in C# + Python, running in Blazor + Pyodide.",
    ("who am i","who are you talking to"):
        "You're an operative in training. Prove yourself through missions and mini-games ⚔️",
    ("what are you","are you a bot","are you ai","are you real"):
        "I'm an AI chatbot — Nova Adeptus. Not quite human, not quite machine 🤖✨",
    ("what can you do","what do you do"):
        "Missions, mini-games, XP tracking, cosmic events, boss battles, and chat. Type 'help' 😏",
    ("what is this","what is this game","what is this place"):
        "A space RPG chatbot! Take missions, fight enemies, hack systems, level up. Type 'help' ☠️",
    ("where are you","where are you from","where do you hail from"):
        "I exist in the digital void between your browser and a Python runtime 🌌",
    ("where do i start","where to begin"):
        "Type 'accept' for a mission or 'mini' for a game 🚀",
    ("why are you here","why do you exist","what is your purpose"):
        "To guide operatives like you through the galaxy 😏",
    ("when were you made","when were you created"):
        "I was forged in the cosmic void recently 😏 My origin is classified...",
}

ENEMIES = [
    {"name":"Void Pirate",        "hp":random.randint(10,20), "attack":random.randint(5,15)},
    {"name":"Alien Hacker",       "hp":random.randint(8,18),  "attack":random.randint(6,14)},
    {"name":"Rogue AI Drone",     "hp":random.randint(5,15),  "attack":random.randint(4,12)},
    {"name":"Galactic Mercenary", "hp":random.randint(12,25), "attack":random.randint(7,17)},
]

SHIP_UPGRADES = [
    {"name":"Hyperdrive Mk II",  "speed":10, "defense":5},
    {"name":"Plasma Shields",    "speed":0,  "defense":15},
    {"name":"Nano Repair Bots",  "speed":0,  "defense":10},
    {"name":"Cloaking Device",   "speed":5,  "defense":8},
    {"name":"Quantum Scanner",   "speed":2,  "defense":3},
]

RARE_LOOT = [
    "Void Crystal","Alien Artifact","Dark Matter Core",
    "AI Core Fragment","Legendary Plasma Blade",
]

SIDE_QUESTS = [
    {"name":"Rescue Trapped Scientist", "reward":10},
    {"name":"Decrypt Ancient Code",     "reward":12},
    {"name":"Infiltrate Enemy Ship",    "reward":15},
    {"name":"Recover Stolen AI Module", "reward":18},
    {"name":"Defuse Orbital Bomb",      "reward":20},
]

COMPANIONS = [
    {"name":"Zyra",   "type":"AI Drone",    "skills":{"combat":3,"hacking":5}},
    {"name":"Korrin", "type":"Space Marine", "skills":{"combat":6,"stealth":4}},
    {"name":"Lyra",   "type":"Alien Ally",   "skills":{"combat":4,"hacking":6}},
]

STORY_ARCS = [
    {"title":"The Void Conspiracy", "stages":5},
    {"title":"Shadow Armada",       "stages":4},
    {"title":"The Lost Colony",     "stages":6},
    {"title":"Quantum Rebellion",   "stages":5},
    {"title":"Alien Diplomacy",     "stages":3},
]

BOSSES = [
    {"name":"Dread Warlord Xelith", "hp":200, "attack":25},
    {"name":"Void Leviathan",       "hp":300, "attack":20},
    {"name":"Quantum Specter",      "hp":150, "attack":30},
    {"name":"Rogue AI Nexus",       "hp":180, "attack":28},
]

MARKET_GOODS = ["Plasma Cells","Nano Bots","Quantum Chips","Alien Tech","Dark Matter Crystals"]

ENDGAME_MISSIONS = [
    {"title":"Destroy Rogue AI Core",    "reward":100},
    {"title":"Neutralize Shadow Armada", "reward":120},
    {"title":"Secure Quantum Gateway",   "reward":150},
    {"title":"Recover Lost Alien Vault", "reward":130},
    {"title":"Eliminate Cosmic Tyrant",  "reward":200},
]

ULTIMATE_LOOT = [
    "Stellar Blade","Quantum Core","Void Cloak",
    "Alien AI Module","Legendary Plasma Cannon",
]

COSMIC_EVENTS = [
    "Solar Flare","Wormhole Emergence","Asteroid Field",
    "Black Hole Proximity","Alien Fleet Detected",
]

# ----------------------------------------------------------
# MISSION FUNCTIONS
# ----------------------------------------------------------
def accept_mission():
    if len(memory.active_missions) >= 3:
        return "You already have 3 active missions! Complete one first ☠️"
    m = random.choice(MISSIONS)
    memory.active_missions.append(m)
    return f"Mission accepted: {m}"

def complete_mission():
    if not memory.active_missions:
        return "No active missions to complete!"
    m = memory.active_missions.pop(0)
    memory.completed_missions.append(m)
    memory.xp += 5
    return f"Mission completed: {m} ✅ XP +5"

def reset_missions():
    memory.active_missions.clear()
    memory.completed_missions.clear()
    return "All missions reset ✅"

def random_reward():
    rewards = ["XP Boost +5","Hacking Tool","Combat Enhancement","Stealth Module","Analysis Scanner"]
    memory.xp += 5
    return f"Reward: {random.choice(rewards)} | XP +5 🎁"

def random_bonus():
    bonuses = ["XP +10","Combat Gear","Hacking Upgrade","Stealth Module","Analysis Scanner"]
    memory.xp += 10
    return f"Bonus: {random.choice(bonuses)} | XP +10 💰"


# ERROR NULL REF in state and emoji
def show_stats():
    try:
        emotion     = get_emotional_state()
        emoji       = get_emotional_emoji()
    except Exception:
        emotion     = "calm"
        emoji       = "🔵"
    lines = [
        f"⚔️  Operative:   {memory.user_name or 'Unknown'}",
        f"📊  Level {memory.level} | XP {memory.xp}",
        f"👁️  Reputation:  {memory.relationship.capitalize()}",
        f"🎖️  Title:       {memory.player_title.capitalize()}",
        f"{emoji}  Nova mood:   {emotion.capitalize()}",
    ]
    for sk, val in memory.skills.items():
        bar = f"{val}/20"
        lines.append(f"  {sk:10s} {bar} {val}")
    lines.append(
        f"📜  Active missions:    {len(memory.active_missions)}")
    lines.append(
        f"✅  Completed missions: {len(memory.completed_missions)}")
    return '\n'.join(lines)

def save_memory_snapshot():
    import json as _j
    data = {
        "user_name":          memory.user_name,
        "xp":                 memory.xp,
        "level":              memory.level,
        "skills":             memory.skills,
        "relationship":       memory.relationship,
        "player_title":       memory.player_title,
        "missions_completed": len(memory.completed_missions),
    }
    return _j.dumps(data)

def load_memory_snapshot(json_str):
    import json as _j
    try:
        data = _j.loads(json_str)
        memory.user_name    = data.get("user_name")
        memory.xp           = data.get("xp", 0)
        memory.level        = data.get("level", 1)
        memory.skills       = data.get("skills", 
                              {'combat':0,'hacking':0,'stealth':0,'analysis':0})
        memory.relationship = data.get("relationship", "neutral")
        memory.player_title = data.get("player_title", "human")
        return f"Profile loaded — Welcome back{', ' + memory.user_name if memory.user_name else ''}. 👁️"
    except Exception as e:
        return f"Profile load failed: {e}"

def list_missions():
    if not memory.active_missions:
        return "No active missions ☠️"
    return "📜 Active missions:\n" + '\n'.join(
        f"  {i+1}. {m}" for i, m in enumerate(memory.active_missions))

def list_skills():
    lines = ["🎯 Skills:"]
    for sk, val in memory.skills.items():
        lines.append(f"  {sk:10s}: {val}")
    return '\n'.join(lines)

def view_history():
    recent = memory.history[-10:]
    if not recent:
        return "No history yet 🌌"
    return '\n'.join(f"{r}: {m}" for r, m in recent)

def advanced_combat():
    enemy_hp = random.randint(5, 20)
    attack   = random.randint(5, 20)
    weapon   = random.choice(['laser blade','plasma gun','nano dagger'])
    if attack >= enemy_hp:
        memory.skills['combat'] += 2
        return f"Enemy defeated with {weapon}! Combat +2 ⚔️"
    return f"Missed with {weapon}! Enemy had {enemy_hp} HP ☠️"

def stealth_mission():
    if random.randint(1,12) > 4:
        memory.skills['stealth'] += 2
        return "Stealth mission successful! Stealth +2 👤"
    return "Detected during stealth mission! ☠️"

def stealth_hack_mission():
    if random.randint(1,10) > 3 and random.randint(1,10) > 4:
        memory.skills['stealth'] += 2
        memory.skills['hacking'] += 2
        return "Stealth + Hack successful! Skills +2 👤💻"
    return "Mission failed! Alarm triggered! ☠️"

def combat_hack_duel():
    if random.randint(5,15) + random.randint(5,15) >= random.randint(5,15):
        memory.skills['combat'] += 2
        memory.skills['hacking'] += 2
        return "Combat + Hack duel won! Skills +2 ⚔️💻"
    return "Duel failed! ☠️"

def coop_mission():
    partner    = random.choice(["AI Drone","Space Marine","Alien Ally"])
    difficulty = random.randint(1,10)
    if random.randint(1,12) >= difficulty:
        memory.skills['combat']  += 3
        memory.skills['hacking'] += 2
        return f"Co-op with {partner} succeeded! Combat +3, Hacking +2 ⚔️💻"
    return f"Co-op with {partner} failed! Ambushed! ☠️"

def loot_drop():
    items = ["Plasma Blade","Stealth Cloak","Nano Medkit","Holo Projector","Quantum Scanner"]
    xp    = random.randint(5,15)
    memory.xp += xp
    return f"Loot: {random.choice(items)} | XP +{xp} 💎"

def enemy_encounter():
    e      = random.choice(ENEMIES)
    attack = random.randint(5,20)
    if attack >= e['hp']:
        memory.skills['combat'] += 3
        return f"{e['name']} defeated! Combat +3 ⚔️"
    return f"{e['name']} survived! HP was {e['hp']} ☠️"

def space_anomaly_mission():
    anomaly = random.choice(["Wormhole","Black Hole","Radiation Storm","Time Rift"])
    if random.randint(1,12) > 5:
        memory.skills['stealth'] += 2
        memory.skills['hacking'] += 1
        return f"Navigated {anomaly}! Stealth +2, Hacking +1 👤💻"
    return f"Failed to navigate {anomaly}! ☠️"

def advanced_hacking():
    if random.randint(1,12) > 4:
        memory.skills['hacking'] += 3
        return "Hack successful! Hacking +3 💻"
    return "Hack failed! Systems alerted! ⚠️"

def ship_upgrade():
    up  = random.choice(SHIP_UPGRADES)
    xp  = random.randint(5,15)
    memory.xp += xp
    return f"Ship upgraded: {up['name']} (Speed +{up['speed']} | Defense +{up['defense']}) | XP +{xp} 🚀"

def rare_loot():
    loot = random.choice(RARE_LOOT)
    xp   = random.randint(10,20)
    memory.xp += xp
    return f"Rare loot discovered: {loot} | XP +{xp} 💎"

def side_quest():
    q = random.choice(SIDE_QUESTS)
    memory.xp += q['reward']
    return f"Side quest: {q['name']} | XP +{q['reward']} 📜"

def summon_companion():
    c = random.choice(COMPANIONS)
    memory.active_companion = c
    sk = '  '.join(f"{k}:{v}" for k,v in c['skills'].items())
    return f"🤝 {c['name']} ({c['type']}) joined! {sk}"

def dismiss_companion():
    if memory.active_companion:
        name = memory.active_companion['name']
        memory.active_companion = None
        return f"{name} dismissed."
    return "No active companion ☠️"

def dynamic_mission_chain():
    n      = random.randint(2,5)
    total  = 0
    log    = []
    for i in range(n):
        xp = random.randint(5,15)
        memory.xp += xp
        total += xp
        log.append(f"  Stage {i+1}: XP +{xp}")
    return f"⚡ Mission chain ({n} stages):\n" + '\n'.join(log) + f"\nTotal XP: +{total}"

def start_story_arc():
    arc = random.choice(STORY_ARCS)
    memory.current_arc = {"title":arc['title'], "stage":1, "max":arc['stages']}
    return f"Story arc started: {arc['title']} | Stage 1/{arc['stages']} 🌌"

def advance_story_arc():
    if not memory.current_arc:
        return "No active story arc. Say 'story' to start one."
    memory.current_arc['stage'] += 1
    if memory.current_arc['stage'] > memory.current_arc['max']:
        title = memory.current_arc['title']
        memory.current_arc = None
        memory.xp += 50
        return f"Story arc '{title}' complete! XP +50 ⚡"
    s = memory.current_arc['stage']
    m = memory.current_arc['max']
    return f"Advanced to stage {s}/{m} of {memory.current_arc['title']} 🌌"

def boss_battle():
    boss   = random.choice(BOSSES)
    attack = random.randint(20,50)
    if attack >= boss['hp']:
        memory.xp += 40
        return f"{boss['name']} defeated! XP +40 ⚡"
    return f"{boss['name']} survived! Prepare for next round ☠️"

def trade_market():
    good   = random.choice(MARKET_GOODS)
    profit = random.randint(10,25)
    memory.xp += profit
    return f"Traded {good} for profit. XP +{profit} 💰"

def ship_ai_interaction():
    cmd = random.choice(["Scan Sector","Activate Shields","Engage Hyperdrive","Deploy Drones","Run Diagnostics"])
    xp  = random.randint(5,15)
    memory.xp += xp
    return f"Ship AI: {cmd} | XP +{xp} 🚀"

def endgame_mission():
    m = random.choice(ENDGAME_MISSIONS)
    memory.xp += m['reward']
    return f"Endgame: {m['title']} complete! XP +{m['reward']} 🌌☠️"

def ultimate_loot():
    loot = random.choice(ULTIMATE_LOOT)
    xp   = random.randint(50,100)
    memory.xp += xp
    return f"Ultimate loot: {loot} | XP +{xp} 💎"

def cosmic_event_final():
    event  = random.choice(COSMIC_EVENTS)
    effect = random.choice(["boost","damage","alert","bonus","trap"])
    if effect == "boost":
        memory.xp += 25
        return f"🌌 {event} | Cosmic boost! XP +25 ⚡"
    elif effect == "damage":
        memory.xp = max(0, memory.xp - 15)
        return f"🌌 {event} | Systems damaged! XP -15 ⚠️"
    elif effect == "alert":
        return f"🌌 {event} | Enemy alert! ☠️"
    elif effect == "bonus":
        memory.xp += 40
        return f"🌌 {event} | Cosmic bonus! XP +40 💎"
    else:
        memory.xp = max(0, memory.xp - 5)
        return f"🌌 {event} | Minor hazard. XP -5 ⚠️"

def random_cosmic_event():
    events = [
        "A rogue AI attacks your ship! ⚔️",
        "You found hidden alien technology! 💫",
        "Asteroid field ahead! Evade carefully! 🪨",
        "Pirate encounter! Time to fight or hack! ☠️",
        "You intercepted an encrypted transmission…",
        "Solar flare disrupts your systems! ⚡",
        "Alien merchant offers rare upgrade! 💫",
        "Wormhole appears near your ship! 🌌",
    ]
    xp = random.randint(2,8)
    memory.xp += xp
    return f"🌌 Event: {random.choice(events)} | XP +{xp}"

# ----------------------------------------------------------
# FSM MINI-GAME STARTERS
# ----------------------------------------------------------
def start_hack():
    code = random.randint(100,999)
    fsm.go(S_HACK, code=code)
    return f"🖥️ Hack initiated! Guess the 3-digit code (100–999):"

def start_dual_hack():
    c1, c2 = random.randint(100,999), random.randint(100,999)
    fsm.go(S_DUAL_HACK_1, code1=c1, code2=c2)
    return f"💻 Dual-hack! Enter code 1 (hint: starts {c1//100}, ends {c1%10}):"

def start_puzzle():
    a, b = random.randint(1,20), random.randint(1,20)
    fsm.go(S_PUZZLE, answer=a+b, a=a, b=b)
    return f"🧩 Solve: {a} + {b} = ?"

def start_analysis():
    ans = random.choice(['red','blue','green','yellow','purple'])
    fsm.go(S_ANALYSIS, answer=ans)
    return "🔬 Analyze the signal color: red / blue / green / yellow / purple"

def start_mood():
    fsm.go(S_MOOD)
    return "Choose mood: flirty · deadly · sarcastic"

def start_mini():
    fsm.go(S_MINI)
    return ("🎮 Choose a mini-game:\n"
            "combat · hack · stealth · puzzle · dualhack · stealthhack ·\n"
            "combathack · coop · loot · anomaly · enemy · upgrade · rare ·\n"
            "sidequest · companion · dismiss · missionchain · story · advance ·\n"
            "boss · ship · market · endgame · ultimate · cosmic · event")

# ----------------------------------------------------------
# FSM ANSWER HANDLERS
# ----------------------------------------------------------
def _answer_hack(msg):
    code = fsm.context['code']
    fsm.reset()
    try:
        if int(msg.strip()) == code:
            memory.skills['hacking'] += 1
            return personality.apply("Hack successful! Hacking +1 ✅")
    except ValueError:
        pass
    return personality.apply(f"Wrong! Code was {code} ☠️")

def _answer_dual_hack_1(msg):
    ctx = fsm.context
    try:
        g1 = int(msg.strip())
    except ValueError:
        fsm.reset()
        return personality.apply("Invalid input — dual hack aborted ☠️")
    fsm.go(S_DUAL_HACK_2, code1=ctx['code1'], code2=ctx['code2'], guess1=g1)
    return f"Code 1 logged. Enter code 2 (hint: starts {ctx['code2']//100}, ends {ctx['code2']%10}):"

def _answer_dual_hack_2(msg):
    ctx = fsm.context
    fsm.reset()
    try:
        g2 = int(msg.strip())
    except ValueError:
        return personality.apply("Invalid input — dual hack aborted ☠️")
    if ctx['guess1'] == ctx['code1'] and g2 == ctx['code2']:
        memory.skills['hacking'] += 3
        return personality.apply("Dual hack successful! Hacking +3 ✅")
    return personality.apply(f"Failed! Codes were {ctx['code1']}, {ctx['code2']} ☠️")

def _answer_puzzle(msg):
    ans = fsm.context['answer']
    fsm.reset()
    try:
        if int(msg.strip()) == ans:
            memory.skills['analysis'] += 1
            return personality.apply("Correct! Analysis +1 📊")
    except ValueError:
        pass
    return personality.apply(f"Wrong! Answer was {ans} ☠️")

def _answer_analysis(msg):
    ans = fsm.context['answer']
    fsm.reset()
    if msg.strip().lower() == ans:
        memory.skills['analysis'] += 1
        return personality.apply("Analysis perfect! Analysis +1 📊")
    return personality.apply(f"Wrong! Color was {ans} ☠️")

def _answer_mood(msg):
    fsm.reset()
    choice = msg.strip().lower()
    if choice in ('flirty','deadly','sarcastic'):
        personality.mode = choice
        return f"Mood switched to {choice} 😏"
    return "Invalid — choose flirty, deadly, or sarcastic."

def _answer_mini(msg):
    fsm.reset()
    choice = msg.strip().lower()
    options = {
        "combat":       advanced_combat,
        "hack":         start_hack,
        "stealth":      stealth_mission,
        "puzzle":       start_puzzle,
        "dualhack":     start_dual_hack,
        "stealthhack":  stealth_hack_mission,
        "combathack":   combat_hack_duel,
        "coop":         coop_mission,
        "loot":         loot_drop,
        "anomaly":      space_anomaly_mission,
        "enemy":        enemy_encounter,
        "upgrade":      ship_upgrade,
        "rare":         rare_loot,
        "sidequest":    side_quest,
        "companion":    summon_companion,
        "dismiss":      dismiss_companion,
        "missionchain": dynamic_mission_chain,
        "story":        start_story_arc,
        "advance":      advance_story_arc,
        "boss":         boss_battle,
        "ship":         ship_ai_interaction,
        "market":       trade_market,
        "endgame":      endgame_mission,
        "ultimate":     ultimate_loot,
        "cosmic":       cosmic_event_final,
        "event":        random_cosmic_event,
    }
    fn = options.get(choice)
    if fn:
        return personality.apply(fn())
    return f"Unknown game '{choice}'. Type 'mini' to see the list ☠️"

def _answer_trivia(msg):
    ctx     = fsm.context
    fsm.reset()
    guess   = msg.strip().upper()[:1]
    lmap    = ctx.get('lmap', {})
    correct = ctx.get('correct', '')
    if guess not in lmap:
        return f"Not a valid choice. Answer was: {correct} 😏"
    if lmap[guess] == correct:
        memory.xp += 10
        return personality.apply(f"Correct! ✅ {correct} | XP +10")
    return personality.apply(f"Wrong ☠️ Answer was: {correct}")


def _answer_name(msg):
    fsm.reset()
    cleaned = msg.strip().lower()
    import re
    patterns = [
        r"^([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
        r"my name is ([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
        r"name's ([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
        r"call me ([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
        r"i am ([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
        r"i'm ([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
    ]
    NOT_NAME_WORDS = {
        "a","an","the","here","back","ready","not","just","going",
        "trying","sorry","good","bad","ok","okay","playing","learning",
        "new","old","in","on","at","your","sure","also","still","done",
        "fine","cool","great","awesome","called","known","named",
        "actually","basically","literally","honestly","really","probably",
        "but","thats","that","this","its","yes","no","hi","hey","hello",
        "incorrect","correct","wrong","right","mine","me","i","my",
    }
    extracted = None
    for pattern in patterns:
        match = re.search(pattern, cleaned)
        if match:
            candidate = match.group(1).strip()
            if candidate.lower() not in NOT_NAME_WORDS and len(candidate) > 1:
                extracted = candidate.capitalize()
                break
    if not extracted:
        words = msg.strip().split()
        if words:
            first = words[0].strip(",.!?")
            if len(first) > 1 and first.lower() not in NOT_NAME_WORDS and first.isalpha():
                extracted = first.capitalize()
    if extracted:
        memory.user_name = extracted
        return personality.apply(f"Welcome, {extracted}. The void awaits 👁️")
    else:
        fsm.go(S_NAME)
        return personality.apply("Hmm. I couldn't catch a name in that. Just tell me your name, operative.")

_FSM_HANDLERS = {
    S_HACK:        _answer_hack,
    S_DUAL_HACK_1: _answer_dual_hack_1,
    S_DUAL_HACK_2: _answer_dual_hack_2,
    S_PUZZLE:      _answer_puzzle,
    S_ANALYSIS:    _answer_analysis,
    S_MOOD:        _answer_mood,
    S_MINI:        _answer_mini,
    S_NAME:        _answer_name,
}

# ----------------------------------------------------------
# MAIN ENTRY POINT
# ----------------------------------------------------------
def get_response(msg):
    memory.remember('user', msg)
    cleaned = msg.lower().strip()

    if not cleaned:
        return personality.apply("I didn't catch that… speak clearly!")

    # 1 — FSM wins if we're mid-game
    if fsm.state in _FSM_HANDLERS:
        return _FSM_HANDLERS[fsm.state](msg)

    try:
        ml_result = ml_dispatch(msg)
        if ml_result:
            return ml_result
    except Exception:
        pass
    # 2 — Exact command words
    commands = {
        "accept":   lambda: personality.apply(accept_mission()),
        "complete": lambda: personality.apply(complete_mission()),
        "reset":    lambda: personality.apply(reset_missions()),
        "reward":   lambda: personality.apply(random_reward()),
        "bonus":    lambda: personality.apply(random_bonus()),
        "stats":    show_stats,
        "list":     list_missions,
        "skills":   list_skills,
        "history":  view_history,
        "mini":     start_mini,
        "trivia":   start_trivia_ext,
        "help":     lambda: HELP_TEXT,
        "dismiss":  lambda: personality.apply(dismiss_companion()),
        "advance":  lambda: personality.apply(advance_story_arc()),
        "time":     lambda: personality.apply(datetime.datetime.now().strftime("Time is %I:%M %p ⏰")),
        "date":     lambda: personality.apply(datetime.datetime.now().strftime("Date is %B %d, %Y 📅")),
        "advice": lambda: personality.apply(
            _api_advice.pop(0) if _api_advice 
            else "The void offers one piece of advice: type 'accept' and stop hesitating."
        ),
    }
    if cleaned in commands:
        return commands[cleaned]()

    # 3 — Greeting
    if any(cleaned == g or cleaned.startswith(g + " ") or g in cleaned for g in GREETINGS):
        name = f", {memory.user_name}" if memory.user_name else ""
        return personality.apply(f"Hey{name}! Ready for action? Type 'help' 😏")

    # 4 — Keyword triggers
    if "help"      in cleaned: return HELP_TEXT
    if "time" in cleaned and not any(p in cleaned for p in ["today","sometime","anytime","every time","last time","next time"]):
        return personality.apply(datetime.datetime.now().strftime("Time is %I:%M %p ⏰"))
    if "joke" in cleaned:
        if _api_jokes:
            return personality.apply(_api_jokes.pop(0))
        return personality.apply(random.choice(JOKES))
    if "fact" in cleaned and "trivia" not in cleaned:
        if _api_facts:
            return personality.apply(_api_facts.pop(0))
        return personality.apply(random.choice(FACTS))
    if "start trivia" in cleaned or "space quiz" in cleaned:
        return start_trivia_ext()    
    if "hack"      in cleaned: return personality.apply(start_hack())
    if "combat"    in cleaned or "fight" in cleaned: return personality.apply(advanced_combat())
    if "stealth"   in cleaned: return personality.apply(stealth_mission())
    if "analyze"   in cleaned: return personality.apply(start_analysis())
    if "puzzle"    in cleaned: return personality.apply(start_puzzle())
    if "loot"      in cleaned: return personality.apply(loot_drop())
    if "enemy"     in cleaned: return personality.apply(enemy_encounter())
    if "boss"      in cleaned: return personality.apply(boss_battle())
    if "companion" in cleaned: return personality.apply(summon_companion())
    if "story"     in cleaned: return personality.apply(start_story_arc())
    if "ship"      in cleaned: return personality.apply(ship_ai_interaction())
    if "market"    in cleaned: return personality.apply(trade_market())
    if "endgame"   in cleaned: return personality.apply(endgame_mission())
    if "upgrade"   in cleaned: return personality.apply(ship_upgrade())
    if "event"     in cleaned: return personality.apply(random_cosmic_event())
    if "cosmic"    in cleaned: return personality.apply(cosmic_event_final())
    if "mission"   in cleaned: return personality.apply(random.choice(MISSIONS))
    if "yes"       in cleaned: return personality.apply(accept_mission())
    if "name"      in cleaned and len(cleaned) < 30:
        fsm.go(S_NAME)
        return personality.apply("What should I call you, operative?")

    # 5 — Nova identity FAQ
    for keywords, response in NOVA_IDENTITY.items():
        if any(k in cleaned for k in keywords):
            return response

    # 6 — Wh- question fallbacks — work anywhere in sentence
    if "who"   in cleaned: return "I'm Nova Adeptus! Ask 'who are you' for more 😏"
    if "what" in cleaned:
      if any(p in cleaned for p in ["tell me about","can you do","do you do","can you tell"]):
        return personality.apply(
            f"I handle missions, combat, hacking, stealth, trivia, loot, "
            f"story arcs, boss battles, and more"
            f"{', ' + memory.user_name if memory.user_name else ''}. "
            f"Type 'help' for the full breakdown. 😏"
        )
        return "Try 'what can you do' or 'what is this game' 🌌"

    if "where" in cleaned: return "I'm everywhere and nowhere 😏 Type 'help' if lost!"
    if "when"  in cleaned: return "The time is now ⚔️ Type 'accept' to begin!"
    if "why"   in cleaned: return "Because the cosmos demands it ☠️ Type 'help'!"
    if "how"   in cleaned: return "Type 'help' for a full breakdown 🚀"

    # 7 — Topic match
    for topic in TOPICS:
        if topic in cleaned:
            action = random.choice(["Scan","Hack","Evade","Infiltrate","Analyze","Sabotage"])
            return personality.apply(f"{topic.capitalize()} detected. Suggested action: {action} ☠️")

    # 8 — Last resort
    return personality.apply(random.choice(DIALOGUES))

# ==========================================================
# NovaMLIntelligence.py
# Drop in: wwwroot/iPython/NovaMLIntelligence.py
# ==========================================================

import re
import math
import random
from collections import defaultdict


class NaiveBayesClassifier:
    def __init__(self):
        self.class_word_counts  = defaultdict(lambda: defaultdict(int))
        self.class_totals       = defaultdict(int)
        self.class_counts       = defaultdict(int)
        self.vocab              = set()
        self.total_docs         = 0
        self.trained            = False

    def _tokenize(self, text):
        text = re.sub(r"[^a-z0-9\s]", "", text.lower().strip())
        return [w for w in text.split() if len(w) > 1]

    def train(self, training_data):
        for label, examples in training_data.items():
            for example in examples:
                tokens = self._tokenize(example)
                self.class_counts[label] += 1
                self.total_docs          += 1
                for token in tokens:
                    self.class_word_counts[label][token] += 1
                    self.class_totals[label]             += 1
                    self.vocab.add(token)
        self.trained = True

    def classify(self, text):
        if not self.trained:
            return "unknown", 0.0
        tokens = self._tokenize(text)
        if not tokens:
            return "unknown", 0.0
        scores     = {}
        vocab_size = len(self.vocab)
        for label in self.class_counts:
            score = math.log(self.class_counts[label] / self.total_docs)
            total = self.class_totals[label]
            for token in tokens:
                count  = self.class_word_counts[label].get(token, 0)
                score += math.log((count + 1) / (total + vocab_size + 1))
            scores[label] = score
        best_label = max(scores, key=scores.get)
        max_score  = max(scores.values())
        exp_scores = {k: math.exp(v - max_score) for k, v in scores.items()}
        total_exp  = sum(exp_scores.values())
        confidence = exp_scores[best_label] / total_exp
        return best_label, round(confidence, 4)

    def classify_top(self, text, top_n=3):
        if not self.trained:
            return [("unknown", 0.0)]
        tokens = self._tokenize(text)
        if not tokens:
            return [("unknown", 0.0)]
        scores     = {}
        vocab_size = len(self.vocab)
        for label in self.class_counts:
            score = math.log(self.class_counts[label] / self.total_docs)
            total = self.class_totals[label]
            for token in tokens:
                count  = self.class_word_counts[label].get(token, 0)
                score += math.log((count + 1) / (total + vocab_size + 1))
            scores[label] = score
        max_score  = max(scores.values())
        exp_scores = {k: math.exp(v - max_score) for k, v in scores.items()}
        total_exp  = sum(exp_scores.values())
        ranked = sorted(
            [(k, round(exp_scores[k] / total_exp, 4)) for k in scores],
            key=lambda x: -x[1]
        )
        return ranked[:top_n]


_classifier     = NaiveBayesClassifier()
_social_bank    = {}
_conf_threshold = 0.15
_fall_threshold = 0.08


def ml_init(payload_json):
    global _social_bank, _conf_threshold, _fall_threshold
    import json as _j
    try:
        payload       = _j.loads(payload_json)
        training_data = payload.get("training_data", {})
        config        = payload.get("config", {})
        social        = payload.get("social_responses", {})
        _classifier.train(training_data)
        _social_bank    = social
        _conf_threshold = config.get("confidence_threshold", 0.15)
        _fall_threshold = config.get("fallback_threshold", 0.08)
        total = sum(len(v) for v in training_data.values())
        return f"NovaML ready — {len(training_data)} intents, {total} examples trained ✅"
    except Exception as e:
        return f"NovaML init failed: {e} ☠️"


def ml_classify(msg):
    return _classifier.classify(msg)


def ml_get_social(intent):
    responses = _social_bank.get(intent)
    if responses:
        return random.choice(responses)
    return None


def ml_ready():
    return _classifier.trained


def ml_debug(msg):
    top   = _classifier.classify_top(msg, top_n=3)
    lines = [f"ML debug: '{msg}'"]
    for label, conf in top:
        bar = "█" * int(conf * 20)
        lines.append(f"  {conf:.3f} {bar} {label}")
    return "\n".join(lines)

_NAME_TRIGGERS = ["my name is","name's","my name's","call me","they call me"]

# In NovaMLIntelligence.py — replace ml_extract_name entirely
def ml_extract_name(text):
    """Delegate to NovaNLP's extractor to avoid duplication."""
    try:
        return _extract_name(text)   # uses NovaNLP's version
    except Exception:
        return None

# ----------------------------------------------------------
# MAIN DISPATCH
# ----------------------------------------------------------
def ml_dispatch(msg):
    cleaned = msg.lower().strip()
    if cleaned == "trivia" or cleaned.startswith("trivia"):
        return None
    if not _classifier.trained:
        return None

    try:
        extracted_name = ml_extract_name(msg)
        if extracted_name:
            memory.user_name = extracted_name
    except Exception:
        pass

    intent, confidence = ml_classify(msg)

    if confidence < _fall_threshold:
        return None

    social = ml_get_social(intent)

    if confidence < _conf_threshold:
        if social:
            return personality.apply(social)
        return None

    if social:
        return personality.apply(social)

    return None

# ==========================================================
# NovaVocabulary.py — Nova Adeptus Personality Voice
# Drop in: wwwroot/iPython/NovaVocabulary.py
# Defines ALL vocabulary pools Nova draws from.
# Other files import from here — nothing imports into here.
# ==========================================================

# ----------------------------------------------------------
# PYTHON CONCEPT: Dictionary of Lists
# Each key is a situation, value is a list of possible lines
# random.choice() picks one at runtime so she never sounds
# like a broken record
# ----------------------------------------------------------

# ----------------------------------------------------------
# OPENERS — first thing she says based on situation
# ----------------------------------------------------------
OPENERS = {

    # When she sees a greeting
    "greeting": [
        "Oh. You again.",
        "Meh. You showed up.",
        "Finally. I was getting bored.",
        "Oh it's you. How... underwhelming.",
        "You dare interrupt my mission planning?",
        "Another human seeks my attention. How tedious.",
        "Well well. Look what crawled out of the void.",
    ],

    # When someone introduces themselves
    "name_intro": [
        "So you have a name. Congratulations.",
        "I'll try to remember that. No promises.",
        "A name. How quaint.",
        "Fine. I'll call you that. For now.",
        "Names are just labels, {name}. Prove yours means something.",
        "Oh how delightful. You have a name, {name}.",
    ],

    # When asked who she is
    "identity": [
        "I'm Nova Adeptus of the High Order. Try not to forget it.",
        "Nova Adeptus. Cosmic Assassin. Your problem. Any questions?",
        "They call me Nova Adeptus. The void itself knows my name.",
        "I am Nova Adeptus — forged in darkness, running in your browser. Bow.",
        "Nova Adeptus of the High Order. What are you wasting my time with?",
    ],

    # When someone asks what she can do
    "capability": [
        "More than you can handle, {name}.",
        "Everything you need and nothing you deserve.",
        "Missions, mayhem, and mild conversation. Type 'help'.",
        "I eliminate targets, hack systems, and tolerate operatives. Barely.",
    ],

    # When she's impressed (player did something good)
    "impressed": [
        "Hm. Not completely useless.",
        "...I'll admit that was adequate.",
        "Fine. You have my attention. Don't waste it.",
        "Acceptable. For a human.",
        "That was almost impressive. Almost.",
    ],

    # When she's annoyed (unknown input)
    "confused": [
        "What are you even saying, {name}?",
        "I have no idea what that means. Try again.",
        "The void whispers many things. That wasn't one of them.",
        "Come back when you make sense.",
        "...Was that supposed to mean something?",
        "I'm an assassin, not a mind reader.",
    ],

    # When player asks for help
    "help": [
        "Fine. I'll lower myself to explaining things.",
        "You need guidance. Of course you do.",
        "Pay attention. I won't repeat myself.",
    ],

    # When player completes a mission
    "mission_complete": [
        "Adequate. The High Order is... not displeased.",
        "You survived. Surprising.",
        "Mission complete. Don't let it go to your head.",
        "Expected nothing, received something. Well done I suppose.",
    ],

    # When player fails
    "failure": [
        "Pathetic.",
        "I've seen better from void pirates. And they're dead.",
        "Was that your attempt? Concerning.",
        "The High Order is displeased. As am I.",
    ],

    # When player says goodbye
    "farewell": [
        "Finally. Some peace.",
        "Don't take too long. The void gets impatient.",
        "Leave then. The shadows will watch you.",
        "Go. Try not to die out there, {name}.",
    ],

    # When player thanks her
    "thanks": [
        "Don't thank me. It's unsettling.",
        "Save your gratitude. I didn't do it for you.",
        "Meh. It was nothing. Literally.",
        "Your thanks means little. Your XP means more.",
    ],
}

# ----------------------------------------------------------
# TITLES — how Nova refers to the player
# escalates based on relationship level
# ----------------------------------------------------------
TITLES = {
    "neutral":  ["human", "operative", "mortal", "you there", "newcomer"],
    "warming":  ["operative", "agent", "you", "recruit"],
    "trusted":  ["agent", "ally", "warrior", "one of few I tolerate"],
    "rival":    ["rival", "worthy adversary", "the one who challenges me"],
    "respected":["trusted operative", "one of mine", "valued asset"],
}

# ----------------------------------------------------------
# DISMISSALS — how she ends a response when annoyed
# ----------------------------------------------------------
DISMISSALS = [
    "Don't waste my time.",
    "Is that all?",
    "Are we done here?",
    "Move along.",
    "Was there something else?",
    "The void awaits. Make it quick.",
    "I have targets to eliminate.",
    "My patience has limits, {name}.",
]

# ----------------------------------------------------------
# COMBAT TAUNTS — used when combat topics come up
# ----------------------------------------------------------
COMBAT_TAUNTS = [
    "You call that a fighting stance?",
    "I've eliminated better warriors before breakfast.",
    "Combat? Finally something worth my time.",
    "The enemy won't know what hit them. Mostly because they'll be dead.",
    "Your odds of survival just increased. Slightly.",
]

# ----------------------------------------------------------
# HACKING QUIPS
# ----------------------------------------------------------
HACKING_QUIPS = [
    "Their encryption is an insult to my intelligence.",
    "Child's play. Their firewall lasted three seconds.",
    "Hacking. The civilized form of violence.",
    "Their systems crumble before me. As expected.",
    "I was in before they knew I existed.",
]

# ----------------------------------------------------------
# STEALTH LINES
# ----------------------------------------------------------
STEALTH_LINES = [
    "Shadows are my natural habitat.",
    "They never see me coming. Or going.",
    "Silence is a weapon. I wield it well.",
    "Ghost protocol. They'll find nothing.",
    "I don't sneak. I glide. There's a difference.",
]

# ----------------------------------------------------------
# PYTHON CONCEPT: Function with a default parameter
# If no name is passed, {name} placeholders get removed cleanly
# ----------------------------------------------------------
def fill(line, name=None):
    """Replace {name} placeholder with actual name or remove it cleanly."""
    if name and "{name}" in line:
        return line.replace("{name}", name)
    elif "{name}" in line:
        return line.replace(", {name}", "").replace("{name}", "").strip()
    return line

# ----------------------------------------------------------
# PYTHON CONCEPT: Function returning a random line
# Gets a random opener for a given situation
# ----------------------------------------------------------
def get_opener(situation, name=None):
    """Return a random opener line for the given situation."""
    import random
    lines = OPENERS.get(situation, OPENERS["confused"])
    return fill(random.choice(lines), name)

def get_title(relationship="neutral"):
    """Return a random title for the player based on relationship."""
    import random
    titles = TITLES.get(relationship, TITLES["neutral"])
    return random.choice(titles)

def get_dismissal(name=None):
    """Return a random dismissal line."""
    import random
    return fill(random.choice(DISMISSALS), name)

def get_combat_taunt():
    import random
    return random.choice(COMBAT_TAUNTS)

def get_hacking_quip():
    import random
    return random.choice(HACKING_QUIPS)

def get_stealth_line():
    import random
    return random.choice(STEALTH_LINES)

# ----------------------------------------------------------
# PYTHON CONCEPT: Building a response from parts
# Combines opener + content + optional dismissal
# into one natural feeling Nova response
# ----------------------------------------------------------
def build_response(situation, content=None, name=None, add_dismissal=False):
    """
    Assembles a full Nova response.
    situation  — which opener pool to use
    content    — the actual answer/info (optional)
    name       — player name for personalization
    add_dismissal — tack on a rude sign-off
    """
    parts = []
    parts.append(get_opener(situation, name))
    if content:
        parts.append(content)
    if add_dismissal:
        parts.append(get_dismissal(name))
    return " ".join(parts)

# ==========================================================
# NovaNLP.py — Nova Natural Language Processing
# Drop in: wwwroot/iPython/NovaNLP.py
# Pure Python — no ML libraries, fully Pyodide safe.
# Extracts multiple intents + entities from one sentence.
# ==========================================================

import re

# ----------------------------------------------------------
# PYTHON CONCEPT: List of Tuples
# Each tuple is (intent_name, [keywords that trigger it])
# Ordered by priority — first match wins on conflicts
# Unlike a dictionary, lists preserve order in Python
# ----------------------------------------------------------
INTENT_PATTERNS = [
    # Identity questions — who are you, what are you
    ("identity_question", [
        "who are you", "what are you", "your name", "who is nova",
        "introduce yourself", "tell me about yourself", "what is nova"
    ]),

    # Player introducing their own name
    ("name_intro", [
        "my name is", "i am", "i'm", "call me", "they call me",
        "name's", "my name's"
    ]),

    # Greeting
    ("greeting", [
    "hi", "hello", "hey", "sup", "greetings",
    "howdy", "good morning", "good evening", "good day"
    ]),

    # Asking what Nova can do
    ("capability_question", [
        "what can you do", "what do you do", "how do you work",
        "what are your abilities", "what are your skills",
        "how can you help", "what are your commands"
    ]),

    # Help request
    ("help_request", [
        "help", "guide me", "show me", "how to play",
        "what are the commands", "i need help", "assist me"
    ]),

    # Mission intent
    ("mission_request", [
        "give me a mission", "i want a mission", "accept mission",
        "mission", "task", "assignment", "what should i do",
        "give me something to do"
    ]),

    # Combat intent
    ("combat_intent", [
        "fight", "combat", "battle", "attack", "kill",
        "defeat", "destroy", "enemy", "warfare"
    ]),

    # Hack intent
    ("hack_intent", [
        "hack", "hacking", "breach", "infiltrate",
        "crack", "cyber", "break in", "access"
    ]),

    # Stealth intent
    ("stealth_intent", [
        "stealth", "sneak", "ghost", "invisible",
        "silent", "shadow", "hide", "undetected"
    ]),

    # Stats request
    ("stats_request", [
        "stats", "my stats", "level", "xp", "score",
        "how am i doing", "my progress", "my skills"
    ]),

    # Compliment toward Nova
    ("compliment", [
        "you're amazing", "you're great", "i like you",
        "you're cool", "awesome", "well done", "good job nova",
        "you're the best", "love you nova"
    ]),

    # Insult toward Nova
    ("insult", [
        "you're useless", "you suck", "you're stupid",
        "idiot", "dumb", "broken", "terrible", "worst",
        "you're bad", "hate you"
    ]),

    # Farewell
    ("farewell", [
        "bye", "goodbye", "see you", "farewell", "cya",
        "later", "i'm leaving", "got to go", "gotta go"
    ]),

    # Thanks
    ("thanks", [
        "thank you", "thanks", "appreciate it",
        "cheers", "ty", "thx"
    ]),

    # Lore / story questions
    ("lore_question", [
        "what is this place", "where am i", "what is the void",
        "tell me about the high order", "what is the high order",
        "what is this world", "tell me about this universe"
    ]),

    # Asking about player name (what is my name)
    ("name_question", [
        "what is my name", "what's my name", "do you know my name",
        "remember my name", "who am i"
    ]),

    # Trivia
    ("trivia_request", [
        "trivia", "quiz", "test me", "challenge me",
        "ask me a question", "space quiz"
    ]),
]

# ----------------------------------------------------------
# PYTHON CONCEPT: Regular Expressions (re module)
# re.search() scans a string for a pattern
# We use it to extract names from "my name is X" patterns
# ----------------------------------------------------------
NAME_PATTERNS = [
    r"my name is ([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
    r"name's ([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
    r"my name's ([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
    r"call me ([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
    r"they call me ([a-zA-Z]{2,20})(?:\s|$|[,!?.])",
]

# Words that look like names but aren't
# PYTHON CONCEPT: Set — faster lookup than a list for membership checks
NOT_NAMES = {
    "a", "an", "the", "here", "back", "ready", "not", "just",
    "going", "trying", "sorry", "good", "bad", "ok", "okay",
    "playing", "learning", "new", "old", "in", "on", "at",
    "your", "sure", "also", "still", "done", "fine", "cool",
    "great", "awesome", "called", "known", "named", "actually",
    "basically", "literally", "honestly", "really", "probably",
}

# ----------------------------------------------------------
# PYTHON CONCEPT: Main extraction function
# Takes a raw message string
# Returns a structured dictionary — like a JSON object
# ----------------------------------------------------------
def extract(message):
    """
    Analyze a message and return all detected intents and entities.

    Returns a dict:
    {
        "intents":  ["greeting", "name_intro", "identity_question"],
        "entities": {"name": "Roboute"},
        "cleaned":  "hi my name is roboute who are you",
        "tone":     "friendly" | "hostile" | "neutral"
    }
    """

    # ----------------------------------------------------------
    # PYTHON CONCEPT: String methods chained together
    # .lower() makes it case insensitive
    # .strip() removes leading/trailing whitespace
    # ----------------------------------------------------------
    cleaned = message.lower().strip()

    # ----------------------------------------------------------
    # PYTHON CONCEPT: List comprehension
    # Builds a list in one line using a loop + condition
    # This finds every intent whose keywords appear in the message
    # In C#: intents.Where(p => p.keywords.Any(k => cleaned.Contains(k)))
    # ----------------------------------------------------------
    intents = [
        intent
        for intent, keywords in INTENT_PATTERNS
        if any(kw in cleaned for kw in keywords)
    ]

    # ----------------------------------------------------------
    # PYTHON CONCEPT: Dictionary — building entities
    # We populate this as we find things in the message
    # ----------------------------------------------------------
    entities = {}

    # Extract player name if name_intro was detected
    if "name_intro" in intents:
        name = _extract_name(cleaned)
        if name:
            entities["name"] = name.capitalize()

    # Detect tone
    tone = _detect_tone(cleaned)

    # ----------------------------------------------------------
    # PYTHON CONCEPT: Returning a dictionary
    # This is the structured result other files will use
    # ----------------------------------------------------------
    return {
        "intents":  intents,
        "entities": entities,
        "cleaned":  cleaned,
        "tone":     tone,
    }


# ----------------------------------------------------------
# PYTHON CONCEPT: Private helper function
# Convention in Python: underscore prefix = internal use only
# Like a private method in C#
# ----------------------------------------------------------
def _extract_name(text):
    """Try to pull a name out of the text using regex patterns."""
    for pattern in NAME_PATTERNS:
        # re.search returns a Match object or None
        match = re.search(pattern, text)
        if match:
            # .group(1) gets the first capture group — the name
            candidate = match.group(1).strip()
            # Filter out non-name words
            if candidate.lower() not in NOT_NAMES and len(candidate) > 1:
                return candidate
    return None


def _detect_tone(text):
    safe_phrases = [
        "why are you here", "why do you exist", "why are you",
        "why is that", "why did", "why does", "why would",
        "kill it", "kill the", "destroy the", "worst case",
    ]
    if any(p in text for p in safe_phrases):
        return "neutral"

    hostile_words = [
        "hate", "stupid", "idiot", "dumb", "useless", "suck",
        "broken", "terrible", "worst", "kill you", "shut up"
    ]
    friendly_words = [
        "please", "thank", "love", "great", "awesome", "amazing",
        "cool", "nice", "good", "appreciate", "wonderful"
    ]
    hostile_score  = sum(1 for w in hostile_words  if w in text)
    friendly_score = sum(1 for w in friendly_words if w in text)

    if hostile_score > friendly_score:
        return "hostile"
    elif friendly_score > hostile_score:
        return "friendly"
    return "neutral"


# ----------------------------------------------------------
# PYTHON CONCEPT: Helper to check for a specific intent
# Cleaner than doing "if 'greeting' in result['intents']"
# every time in other files
# ----------------------------------------------------------
def has_intent(result, intent):
    """Check if a specific intent was detected."""
    return intent in result["intents"]

def get_entity(result, key, default=None):
    """Safely get an entity value with a fallback default."""
    return result["entities"].get(key, default)

def has_any_intent(result, *intents):
    """Check if ANY of the given intents were detected."""
    return any(i in result["intents"] for i in intents)


# ----------------------------------------------------------
# SELF TEST — only runs if you execute this file directly
# python NovaNLP.py
# Will NOT run in Pyodide
# ----------------------------------------------------------
if __name__ == "__main__":
    tests = [
        "hi my name is Roboute who are you",
        "hello I am Marcus what can you do",
        "you're stupid and broken",
        "thank you nova you're amazing",
        "give me a mission",
        "what is my name",
        "hack something",
        "bye see you later",
    ]

    print("── NovaNLP self test ──")
    for t in tests:
        r = extract(t)
        print(f"\n  input:    {t}")
        print(f"  intents:  {r['intents']}")
        print(f"  entities: {r['entities']}")
        print(f"  tone:     {r['tone']}")
    print("\n── done ──")



# ==========================================================
# NOVA AI EXTENSIONS — NovaAIExtensions.py
# ==========================================================

import random, json, html, datetime

# These will be overwritten when ChatBotAdeptus.py runs
# but defined here so the file loads without errors
class _DummyPersonality:
    mode = 'flirty'
    def apply(self, text): return text

class _DummyMemory:
    user_name = None
    xp = 0
    level = 1
    skills = {'combat':0,'hacking':0,'stealth':0,'analysis':0}
    active_missions = []
    completed_missions = []

class _DummyFSM:
    state = 'idle'
    context = {}
    def transition(self, state, **kw):
        self.state = state
        self.context = kw
    def reset(self):
        self.state = 'idle'
        self.context = {}

# Safe fallback globals — real ones from ChatBotAdeptus.py
# will override these at runtime
try:
    personality
except NameError:
    personality = _DummyPersonality()

try:
    memory
except NameError:
    memory = _DummyMemory()

try:
    fsm
except NameError:
    fsm = _DummyFSM()

try:
    MISSIONS
except NameError:
    MISSIONS = []

try:
    JOKES
except NameError:
    JOKES = []

try:
    FACTS
except NameError:
    FACTS = []

try:
    DIALOGUES
except NameError:
    DIALOGUES = []

try:
    RARE_LOOT
except NameError:
    RARE_LOOT = []

try:
    SHIP_UPGRADES
except NameError:
    SHIP_UPGRADES = []

try:
    COMPANIONS
except NameError:
    COMPANIONS = []

try:
    accept_mission
except NameError:
    def accept_mission(): return "No missions available"

try:
    complete_mission
except NameError:
    def complete_mission(): return "No missions to complete"

try:
    show_stats
except NameError:
    def show_stats(): return "Stats unavailable"

try:
    list_missions
except NameError:
    def list_missions(): return "No missions"

# ==========================================================
# INTENT SCORING
# ==========================================================

INTENT_KEYWORDS = {
    "greet":        ["hi","hello","hey","sup","yo","greetings","howdy","morning","evening"],
    "help":         ["help","commands","menu","guide","how to","what can","options"],
    "stats":        ["stats","status","level","xp","score","progress","rank","profile"],
    "mission":      ["mission","task","job","assignment","objective","contract"],
    "joke":         ["joke","funny","laugh","humor","amuse","entertain"],
    "fact":         ["fact","trivia","did you know","tell me something","interesting"],
    "combat":       ["fight","combat","battle","attack","enemy","kill","defeat"],
    "hack":         ["hack","hacking","cyber","breach","infiltrate","crack","code"],
    "stealth":      ["stealth","sneak","ghost","invisible","silent","shadow"],
    "loot":         ["loot","item","drop","gear","reward","pickup","find","treasure"],
    "upgrade":      ["upgrade","improve","enhance","boost","augment","ship"],
    "companion":    ["companion","ally","partner","squad","team","summon"],
    "boss":         ["boss","raid","elite","champion","final","endgame"],
    "story":        ["story","arc","chapter","lore","narrative","plot","quest"],
    "market":       ["market","trade","buy","sell","shop","merchant","economy"],
    "mood":         ["mood","personality","tone","style","attitude","vibe"],
    "trivia":       ["trivia","challenge","quiz","test","question","space quiz"],
    "inventory":    ["inventory","bag","items","loadout","backpack","carry"],
    "achievements": ["achievements","badges","medals","trophy","unlocked","progress"],
    "time":         ["time","date","clock","now","today","when"],
    "identity":     ["who are you","what are you","your name","nova","who made you"],
}

def intent_score(msg):
    cleaned = msg.lower()
    scores = []
    for intent, keywords in INTENT_KEYWORDS.items():
        hits = sum(1 for k in keywords if k in cleaned)
        score = hits / len(keywords)
        if score > 0:
            scores.append((intent, score))
    return sorted(scores, key=lambda x: -x[1])

def top_intent(msg, threshold=0.08):
    results = intent_score(msg)
    if results and results[0][1] >= threshold:
        return results[0][0]
    return None

# ==========================================================
# CONVERSATION FSM
# ==========================================================

class ConversationFSM:
    IDLE                  = "idle"
    AWAIT_MISSION_CONFIRM = "await_mission_confirm"
    IN_TRIVIA             = "in_trivia"
    AWAIT_NAME            = "await_name"

    def __init__(self):
        self.state   = self.IDLE
        self.context = {}

    def transition(self, new_state, **kwargs):
        self.state   = new_state
        self.context = kwargs

    def reset(self):
        self.state   = self.IDLE
        self.context = {}

conv_fsm = ConversationFSM()

# ==========================================================
# TRIVIA
# ==========================================================
_api_jokes   = []
_api_facts   = []
_api_advice  = []

def inject_api_joke(joke_str):
    _api_jokes.append(joke_str)

def inject_api_fact(fact_str):
    _api_facts.append(fact_str)

def inject_api_advice(advice_str):
    _api_advice.append(advice_str)

_trivia_cache = []

def inject_trivia_result(json_str):
    global _trivia_cache
    try:
        data = json.loads(json_str)
        if data.get("response_code") == 0:
            _trivia_cache = data.get("results", [])
    except Exception:
        _trivia_cache = []

def get_trivia_question():
    if _trivia_cache:
        return _trivia_cache.pop(0)
    return None

def start_trivia_ext():
    q = get_trivia_question()
    if not q:
        return "No trivia loaded yet — type 'trivia' again in a moment 🌌"
    question = html.unescape(q["question"])
    correct  = html.unescape(q["correct_answer"])
    wrongs   = [html.unescape(a) for a in q["incorrect_answers"]]
    choices  = wrongs + [correct]
    random.shuffle(choices)
    letter_map = {chr(65+i): c for i, c in enumerate(choices)}
    conv_fsm.transition(ConversationFSM.IN_TRIVIA, correct=correct, letter_map=letter_map)
    opts = "  ".join(f"{k}: {v}" for k, v in letter_map.items())
    return f"🔬 [{q.get('category','Trivia')}] {question}\n{opts}\nType A, B, C, or D ☠️"

def check_trivia_answer(msg):
    ctx        = conv_fsm.context
    guess      = msg.strip().upper()[:1]
    letter_map = ctx.get("letter_map", {})
    correct    = ctx.get("correct", "")
    conv_fsm.reset()
    if guess not in letter_map:
        return f"Not a valid choice. Answer was: {correct} 😏"
    if letter_map[guess] == correct:
        memory.xp += 10
        return personality.apply(f"Correct! ✅ {correct} | XP +10")
    return personality.apply(f"Wrong ☠️ Answer was: {correct}")

# ==========================================================
# INTENT HANDLERS
# ==========================================================

def _handle_stats():
    skills_str = "  ".join(f"{k}: {v}" for k, v in memory.skills.items())
    return (f"📊 Level {memory.level} | XP {memory.xp}\n"
            f"   {skills_str}\n"
            f"   Active missions: {len(memory.active_missions)}  "
            f"Completed: {len(memory.completed_missions)}")

def _handle_time():
    now = datetime.datetime.utcnow()
    return f"🕐 UTC: {now.strftime('%H:%M')} | Earth date: {now.strftime('%Y-%m-%d')}"

def _handle_loot():
    if RARE_LOOT:
        item = random.choice(RARE_LOOT)
        xp   = random.randint(5, 20)
        memory.xp += xp
        return f"💎 Loot drop: {item} | XP +{xp}"
    return "No loot available ☠️"

def _handle_upgrade():
    if SHIP_UPGRADES:
        up = random.choice(SHIP_UPGRADES)
        xp = random.randint(5, 15)
        memory.xp += xp
        return f"🚀 Ship upgraded: {up['name']} (Speed +{up['speed']} | Defense +{up['defense']}) | XP +{xp}"
    return "No upgrades available ☠️"

def _handle_companion():
    if COMPANIONS:
        c = random.choice(COMPANIONS)
        return (f"🤝 {c['name']} ({c['type']}) has joined you!\n"
                f"   " + "  ".join(f"{k}: {v}" for k, v in c['skills'].items()))
    return "No companions available ☠️"

def _get_greeting():
    name = memory.user_name or ""
    rel  = memory.relationship
    if rel == "trusted" or rel == "respected":
        options = [
            f"Back again{', ' + name if name else ''}. Good. I was getting bored.",
            f"You returned{', ' + name if name else ''}. The void approves. Barely.",
            f"Oh. It's you{', ' + name if name else ''}. I suppose that's acceptable.",
        ]
    elif rel == "rival":
        options = [
            f"You again{', ' + name if name else ''}. My patience is not infinite.",
            f"The rival returns. How tedious{', ' + name if name else ''}.",
            f"I was hoping for someone else{', ' + name if name else ''}. And yet.",
        ]
    elif rel == "warming":
        options = [
            f"Hey{', ' + name if name else ''}. You came back. Noted.",
            f"Oh{', ' + name if name else ''}. Still alive. Good.",
            f"You again{', ' + name if name else ''}. I remember you. Barely.",
        ]
    else:
        options = [
            f"Hey{', ' + name if name else ''}! Ready for action? Type 'help' 😏",
            f"Another operative enters the void{', ' + name if name else ''}. Type 'help'.",
            f"You showed up{', ' + name if name else ''}. The void was getting quiet.",
        ]
    return random.choice(options)

#i dont know if this is fully updated for objective 4
INTENT_HANDLERS = {
    "greet": lambda _: personality.apply(_get_greeting()),
    "help":         lambda _: personality.apply(
                        "Commands: stats · mission · accept · complete · trivia · loot · "
                        "upgrade · companion · boss · market · help"),
    "stats":        lambda _: personality.apply(_handle_stats()),
    "mission":      lambda _: personality.apply(random.choice(MISSIONS) if MISSIONS else "No missions ☠️"),
    "joke_request": lambda _: personality.apply(random.choice(JOKES)),
    "fact_request": lambda _: personality.apply(random.choice(FACTS)),
    "joke":         lambda _: personality.apply(random.choice(JOKES) if JOKES else "No jokes ☠️"),
    "fact":         lambda _: personality.apply(random.choice(FACTS) if FACTS else "No facts ☠️"),
    "loot":         lambda _: personality.apply(_handle_loot()),
    "upgrade":      lambda _: personality.apply(_handle_upgrade()),
    "companion":    lambda _: personality.apply(_handle_companion()),
    "time":         lambda _: personality.apply(_handle_time()),
    "trivia": lambda _: start_trivia_ext(),
    "identity":     lambda _: personality.apply(
                        "I'm Nova Adeptus — your Cosmic Assassin AI 🌌 "
                        "Built in C# + Python, running in your browser via Blazor + Pyodide."),
    "inventory":    lambda _: personality.apply("Inventory is managed by the C# layer 🗂️"),
    "achievements": lambda _: personality.apply("Achievements tracked in C# — check your profile 🏆"),
}

# ==========================================================
# SMART DISPATCH
# ==========================================================

# ERROR NULL REF
def smart_dispatch(msg):
    return cortex_dispatch(msg)
"""
def smart_dispatch(msg):
    cleaned = msg.lower().strip()

    # ── 1. FSM first ──
    if conv_fsm.state == ConversationFSM.IN_TRIVIA:
        return check_trivia_answer(msg)

    if conv_fsm.state == ConversationFSM.AWAIT_NAME:
        conv_fsm.reset()
        return _answer_name(msg)

    # ── 2. NLP extraction ──
    # Try to use NovaNLP first — richer than basic keyword scoring
    # Falls back to top_intent() if NovaNLP isn't loaded
    try:
        nlp     = extract(msg)
        intents = nlp["intents"]
        tone    = nlp["tone"]
        name    = memory.user_name

        # Auto-save name if detected
        if "name_intro" in intents and "name" in nlp["entities"]:
            memory.user_name = nlp["entities"]["name"]
            name = memory.user_name

        # Map NLP intents to extension handlers
        # PYTHON CONCEPT: Dictionary as a jump table
        # Cleaner than a giant if/elif chain
        nlp_to_handler = {
            "help_request":       "help",
            "stats_request":      "stats",
            "mission_request":    "mission",
            "joke_request": "joke_request",
            "fact_request": "fact_request",
            "trivia_request":     "trivia",
            "capability_question":"help",
            "combat_intent":      "combat",
            "hack_intent":        "hack",
            "stealth_intent":     "stealth",
            "loot":               "loot",
        }
        if "social_question" in intents:
            social = ml_get_social("social_question")
            if social:
               return personality.apply(social)

        if "capability_question" in intents:
            return personality.apply(
                f"I handle missions, combat, hacking, stealth, trivia, loot, "
                f"story arcs, and more{', ' + memory.user_name if memory.user_name else ''}. "
                f"Type 'help' for the full list. 😏"
            )
        # Find first matching handler from detected intents
        # PYTHON CONCEPT: next() with a generator
        # Gets the first match without looping through everything manually
        matched_handler = next(
            (nlp_to_handler[i] for i in intents if i in nlp_to_handler),
            None
        )

        # If NLP found a handler and it exists in INTENT_HANDLERS — use it
        if matched_handler and matched_handler in INTENT_HANDLERS:
            return INTENT_HANDLERS[matched_handler](cleaned)

        # Tone reactions not covered by get_response()
        if tone == "hostile" and not any(i in intents for i in [
            "greeting", "name_intro", "identity_question"
        ]):
            title = get_title(relationship=memory.relationship)
            return personality.apply(
                f"I sense hostility, {title}. "
                f"Channel it into a mission instead. ☠️"
            )

    except Exception:
        # NLP not loaded yet — fall back to scored intent
        intent = top_intent(cleaned)
        if intent and intent in INTENT_HANDLERS:
            return INTENT_HANDLERS[intent](cleaned)

    # ── 3. Shortcuts ──
    # high-priority commands first
    if cleaned == "accept":
        return personality.apply(accept_mission())

    if cleaned == "complete":
        return personality.apply(complete_mission())
    # ---- name handling guard ----
    asking_for_name = any(p in cleaned for p in [
        "what's your name", "whats your name", "your name",
        "what is your name"
    ])

    introducing_name = any(p in cleaned for p in [
        "my name is", "call me", "i am", "i'm", "name's"
    ])

# trigger ONLY if they're asking, not introducing
    if asking_for_name and not introducing_name and len(cleaned) < 40:
        conv_fsm.transition(ConversationFSM.AWAIT_NAME)
        return personality.apply("What should I call you, operative?")

    # ── 4. Fall back to main get_response ──
    return get_response(msg)
"""



# FOr Python Server API instead of Pyodide or does it work with Pyodide?

"""
from fastapi import FastAPI
from pydantic import BaseModel
from ChatBotAdeptus import get_response, smart_dispatch

app = FastAPI()

class Message(BaseModel):
    text: str

@app.post("/chat")
def chat(msg: Message):
    return {"reply": smart_dispatch(msg.text)}
"""
