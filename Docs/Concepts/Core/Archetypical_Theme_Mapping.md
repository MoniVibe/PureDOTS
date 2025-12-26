# Archetypical Theme Mapping

**Status:** Draft  
**Category:** Core / Aesthetics / Identity  
**Applies To:** Godgame, Space4X, shared PureDOTS  
**Created:** 2025-01-XX  
**Last Updated:** 2025-01-XX

---

## Purpose

This document maps combinations of **alignments**, **outlooks**, and **behaviors** to archetypical themes, aesthetics, and visual expressions. These mappings inform:
- Visual design (colors, materials, architecture, clothing)
- Narrative tone and cultural expressions
- UI/UX styling for entity presentation
- Audio/music direction
- Presentation layer theming

**Core Principle:** Every combination of alignment + outlook + behavior creates a unique archetypical expression that should be visually and thematically distinct.

---

## Overview: Alignments, Outlooks, and Behavior Profiles

### What Are These Systems?

**Alignments, outlooks, and behaviors** are semantic signals that define an entity's values, cultural expressions, and personality traits. They work together to create a complete archetypical profile that determines how entities think, act, and express themselves.

### Alignment (Moral/Ideological Position)

**Alignment** represents **WHAT** an entity believes morally and ideologically. It uses a **tri-axis system**:

- **Moral Axis:** Good (+100) ↔ Neutral (0) ↔ Evil (-100)
  - Good: Beneficial effect on others, helps, protects, nurtures
  - Evil: Harmful effect on others, exploits, destroys, corrupts
  - Neutral: Neither helpful nor harmful, indifferent, transactional, contextual

- **Order Axis:** Lawful (+100) ↔ Neutral (0) ↔ Chaotic (-100)
  - Lawful: Structured, hierarchical, disciplined, traditional
  - Chaotic: Free, unpredictable, organic, wild
  - Neutral: Balanced, practical, adaptable, context-dependent

- **Purity Axis:** Pure (+100) ↔ Neutral (0) ↔ Corrupt (-100)
  - Pure: Selfless commitment to ideals, grander purpose, principled
  - Corrupt: Selfish, "by any means necessary," personal gain, exploitative
  - Neutral: Balanced self-interest, transactional, fair exchange

**Alignment Strength:** Each alignment has a **strength value (0-1)** indicating how strongly held the beliefs are. Higher strength = more resistant to change.

**Aggregate Alignment:** For groups (villages, fleets, guilds), alignment is computed as a weighted average of member alignments, with leaders and influential members having higher weight.

### Outlooks (Cultural/Behavioral Expression)

**Outlooks** represent **HOW** alignment is expressed culturally and behaviorally. They are the concrete manifestation of abstract alignment values.

**Outlook System:**
- Entities can have **up to 3 regular outlooks** OR **2 fanatic outlooks** (extreme, locked positions)
- All outlooks exist simultaneously, but only the top 3 (or top 2 if fanatic threshold crossed) are counted for aggregation
- Outlooks are flexible - no hardcoded combinations, games interpret them as needed

**Core Outlook Axes:**
- **Xenophobia/Xenophilia:** Isolation vs integration, traditional vs cosmopolitan
- **Authoritarian/Egalitarian:** Hierarchy vs equality, structured vs horizontal
- **Materialist/Spiritual:** Wealth vs faith, industrial vs transcendent
- **Warlike/Peaceful:** Combat vs diplomacy, aggression vs harmony
- **Expansionist/Isolationist:** Growth vs stability, outward vs inward
- **Scholarly/Artisan:** Knowledge vs craft, academic vs creative

**Outlook Combinations:**
- **3 Regular Outlooks:** More nuanced, blended cultural expressions
- **2 Fanatic Outlooks:** Extreme positions, locked values, stronger cultural identity
- Combinations create unique archetypes (e.g., Warlike + Xenophobic = "Xenocidal Purgers")

**Aggregate Outlooks:** Groups derive their outlooks from member contributions, with dominant outlooks emerging from the collective.

### Behavior Profiles (Personality Traits)

**Behavior profiles** represent **HOW** an entity reacts emotionally and makes decisions. They are independent of alignment and outlook - they determine response patterns, not values.

**Core Behavior Axes:**
- **Vengeful/Forgiving:** Grudge-holding vs reconciliation, retribution vs mercy
- **Bold/Craven:** Risk-taking vs caution, confidence vs fear
- **Cooperative/Competitive:** Collective action vs individual achievement
- **Might/Magic Affinity:** Preference for physical/tech power vs mystical/psionic power

**Behavior Effects:**
- **Initiative:** Bold entities act more frequently, craven less so
- **Combat Stance:** Bold = aggressive, craven = defensive
- **Social Dynamics:** Vengeful = hold grudges, forgiving = quick to reconcile
- **Power Expression:** Might = physical/tech focus, Magic = mystical/psionic focus

**Behavior vs Alignment:**
- **Alignment:** WHAT they value (Good/Evil, Lawful/Chaotic)
- **Behavior:** HOW they react (Bold/Craven, Vengeful/Forgiving)
- Example: A Good Vengeful entity values helping others but holds grudges against those who harm them

### Profile Interaction

**How They Work Together:**
1. **Alignment** sets the moral/ideological foundation (WHAT they believe)
2. **Outlooks** express alignment culturally (HOW alignment manifests)
3. **Behaviors** determine reaction patterns (HOW they respond)

**Example Profile:**
- **Alignment:** Lawful Good Pure (+70 moral, +80 order, +60 purity)
- **Outlooks:** Warlike, Xenophobic, Authoritarian
- **Behaviors:** Bold, Vengeful, Might-focused
- **Result:** A principled warrior who fights with honor, defends their people, holds grudges against enemies, and prefers physical combat

### Profile Evolution

**Dynamic Profiles:**
- Alignment can shift based on actions, experiences, and external influence
- Outlooks can change as culture evolves or entities are exposed to new ideas
- Behaviors are more stable but can drift over time based on experiences

**Drift Mechanics:**
- **Alignment Drift:** Actions create "footprints" that shift alignment values
- **Outlook Drift:** Cultural exposure, leadership changes, population shifts
- **Behavior Drift:** Experiences modify personality traits (slow, resistant to change)

**Resistance:**
- Extreme values (near -100 or +100) resist change more strongly
- High alignment strength increases resistance
- Fanatic outlooks are locked and resist change

### Aggregate Profiles (Groups)

**How Groups Work:**
- **Aggregate Alignment:** Weighted average of member alignments
- **Aggregate Outlooks:** Dominant outlooks from member composition (top 3 regular or top 2 fanatic)
- **Aggregate Behaviors:** Majority vote or weighted average of member behaviors
- **Cohesion:** How unified the group is (affects how strongly aggregate profile influences members)

**Group Influence:**
- Strong aggregate profiles influence individual members (cultural pressure)
- Individual members can influence aggregate (leadership, majority shifts)
- High cohesion = stronger collective identity, less individual deviation

### Hive Mind Profiles

**Shared Consciousness:**
- Hive-minded entities share alignment/outlook/behavior profiles at aggregate level
- Individual drones synchronize with collective profile
- Deviation possible but handled based on hive mind's profile (purge, allow plurality, combat resolution)

### Profile Semantics

**PureDOTS Provides:**
- Continuous axis values (not discrete enums)
- Semantic signals (alignment/outlook/behavior axes)
- No hardcoded archetype names or combinations
- Flexible interpretation by games

**Games Interpret:**
- Map axis values to visual themes, aesthetics, naming
- Create archetype labels based on combinations
- Define policies, edicts, cultural expressions
- Generate narratives, dialogues, behaviors

**Key Principle:** PureDOTS provides the **semantic foundation** (what the values mean), games provide the **presentation layer** (how they're expressed visually and thematically).

---

## PureDOTS Runtime Contract (Theme-Agnostic)

PureDOTS provides only the semantic signal:
- `ArchetypeFlavor` (runtime component) is a normalized axis vector derived from alignment, outlook, personality, and power preference.
- No explicit combinations or named archetypes are encoded in runtime. Combinations are organic and emerge from the continuous axis values.
- Games (Space4X, Godgame) map `ArchetypeFlavor` to presentation assets, palettes, and naming without feeding theme data back into PureDOTS.

---

## Alignment Axes & Aesthetic Expressions

### Order Axis (Lawful ↔ Chaotic)

**Lawful (+100):**
- **Aesthetics:** Roman, civil, structured, architectural
- **Themes:** Order, hierarchy, discipline, tradition, protocol
- **Visual:** Clean lines, symmetrical designs, formal structures, classical architecture
- **Materials:** Marble, polished stone, structured metals, formal textiles
- **Colors:** Deep blues, purples, golds (regal, formal)

**Chaotic (-100):**
- **Aesthetics:** Aztec, savage, organic, wild
- **Themes:** Freedom, unpredictability, natural chaos, primal energy
- **Visual:** Asymmetric patterns, organic curves, jagged edges, natural forms
- **Materials:** Raw stone, bone, hide, unrefined metals, natural fibers
- **Colors:** Reds, oranges, earth tones (primal, energetic)

**Neutral (0):**
- **Aesthetics:** Balanced, practical, adaptable
- **Themes:** Pragmatic, flexible, context-dependent
- **Visual:** Simple, functional designs, neither rigid nor wild
- **Materials:** Common materials, versatile construction
- **Colors:** Grays, muted tones, balanced palettes

### Purity Axis (Pure ↔ Corrupt)

**Pure (+100):**
- **Expression:** Selfless commitment to ideals, grander purpose
- **Pure Warlike:** Committed to combat for honor/defense, fights with principles
- **Pure Materialist:** Values everything, sees worth in all things, sustainable
- **Pure Peaceful:** Selfless caretaking, sacrifices for others
- **Aesthetics:** Clean, elevated, aspirational, noble
- **Visual:** Refined, elegant, purposeful, principled
- **Themes:** Altruism, dedication, higher purpose, integrity

**Corrupt (-100):**
- **Expression:** Selfish, "by any means necessary," personal gain
- **Corrupt Warlike:** Selfish victory, no honor, backstabbing, mercenary
- **Corrupt Materialist:** No problem enslaving others, exploitation, hoarding
- **Corrupt Peaceful:** Passive-aggressive, manipulative, false peace
- **Aesthetics:** Decadent, twisted, self-serving, opportunistic
- **Visual:** Ornate but tarnished, gilded but hollow, excessive
- **Themes:** Self-interest, exploitation, manipulation, greed

**Neutral (0):**
- **Expression:** Balanced self-interest, transactional, fair exchange
- **Aesthetics:** Practical, honest, straightforward
- **Visual:** Clear, unadorned, functional
- **Themes:** Mutual benefit, fair trade, balanced relationships

### Moral Axis (Good ↔ Evil)

**Good (+100):**
- **Expression:** Beneficial effect on others, helps, protects, nurtures
- **Aesthetics:** Warm, welcoming, protective, nurturing
- **Visual:** Soft edges, warm lighting, inviting spaces, safe havens
- **Colors:** Warm greens, soft blues, gentle yellows, pastels
- **Themes:** Protection, growth, healing, community, benevolence

**Evil (-100):**
- **Expression:** Harmful effect on others, exploits, destroys, corrupts
- **Aesthetics:** Cold, threatening, oppressive, destructive
- **Visual:** Sharp edges, harsh lighting, dangerous spaces, fortresses
- **Colors:** Dark reds, deep purples, blacks, cold grays
- **Themes:** Exploitation, destruction, domination, suffering

**Neutral (0):**
- **Expression:** Neither helpful nor harmful, indifferent, transactional
- **Aesthetics:** Neutral, balanced, neither inviting nor threatening
- **Visual:** Moderate, balanced, functional
- **Colors:** Grays, browns, muted tones
- **Themes:** Indifference, neutrality, pragmatism

---

## Outlook Axes & Aesthetic Expressions

### Xenophobia/Xenophilia Axis

**Xenophobic (-100):**
- **Aesthetics:** Puritan, conservative, insular, traditional
- **Visual:** Closed forms, defensive architecture, traditional patterns, restricted spaces
- **Themes:** Isolation, purity, tradition, exclusion, protection from outsiders
- **Materials:** Local materials only, traditional crafts, defensive structures
- **Colors:** Muted, conservative, earth tones, traditional palettes

**Xenophilic (+100):**
- **Aesthetics:** Humane, artistic, cosmopolitan, open
- **Visual:** Open forms, welcoming architecture, diverse patterns, inclusive spaces
- **Themes:** Integration, diversity, cultural exchange, openness, curiosity
- **Materials:** Mixed materials, imported goods, artistic fusion
- **Colors:** Vibrant, diverse, international palettes, rich hues

**Neutral (0):**
- **Aesthetics:** Balanced, cautious but not closed, open but not naive
- **Visual:** Moderate openness, selective integration
- **Themes:** Pragmatic relations, selective exchange

### Authoritarian/Egalitarian Axis

**Authoritarian (-100):**
- **Aesthetics:** Knightly hierarchies, feudal, structured power
- **Visual:** Vertical hierarchies, clear ranks, formal structures, power symbols
- **Themes:** Order through hierarchy, clear roles, chain of command, honor codes
- **Materials:** Precious metals for elites, clear material distinctions
- **Colors:** Rich colors for elites, muted for commoners, clear stratification

**Egalitarian (+100):**
- **Aesthetics:** Social equality, democratic, horizontal structures
- **Visual:** Horizontal layouts, shared spaces, equal access, communal designs
- **Themes:** Equal rights, shared power, collective decision-making, mutual respect
- **Materials:** Shared resources, equal quality, communal ownership
- **Colors:** Uniform palettes, shared aesthetics, no class distinction

**Neutral (0):**
- **Aesthetics:** Meritocratic, balanced hierarchy, flexible structures
- **Visual:** Moderate hierarchy, earned distinctions, flexible roles
- **Themes:** Merit-based advancement, balanced power

### Materialist/Spiritual Axis

**Materialist (-100):**
- **Aesthetics:** Brutalist, industrial, functional, pragmatic
- **Visual:** Raw concrete, exposed structures, industrial forms, utilitarian
- **Themes:** Wealth, production, efficiency, tangible value, commerce
- **Materials:** Concrete, steel, industrial materials, mass production
- **Colors:** Grays, industrial tones, metallic, functional palettes

**Spiritual (+100):**
- **Aesthetics:** Elegant, refined, transcendent, ethereal
- **Visual:** Flowing forms, delicate structures, ornate details, elevated designs
- **Themes:** Faith, transcendence, ritual, higher purpose, inner value
- **Materials:** Precious stones, fine fabrics, sacred materials, handcrafted
- **Colors:** Golds, silvers, whites, ethereal palettes, luminous tones

**Neutral (0):**
- **Aesthetics:** Balanced, practical but respectful, functional but meaningful
- **Visual:** Moderate refinement, balanced materials
- **Themes:** Balanced values, practical spirituality

### Warlike/Peaceful Axis

**Warlike (+100):**
- **Aesthetics:** Brutal mercs, militaristic, aggressive, combat-focused
- **Visual:** Armor, weapons, fortifications, military structures, defensive designs
- **Themes:** Combat, conquest, honor through battle, martial prowess, strength
- **Materials:** Metals, leather, defensive materials, weapon-grade resources
- **Colors:** Reds, dark metals, war tones, aggressive palettes

**Peaceful (-100):**
- **Aesthetics:** Philosophers in togas, contemplative, nurturing, diplomatic
- **Visual:** Flowing robes, open spaces, gardens, libraries, peaceful structures
- **Themes:** Diplomacy, healing, knowledge, harmony, growth
- **Materials:** Soft fabrics, natural materials, peaceful resources, gentle textures
- **Colors:** Soft blues, greens, pastels, calming palettes

**Neutral (0):**
- **Aesthetics:** Balanced, defensive but not aggressive, prepared but peaceful
- **Visual:** Moderate defenses, balanced designs
- **Themes:** Prepared defense, balanced approach

---

## Behavior Axes & Aesthetic Expressions

### Might/Magic Affinity Axis

**Might (-100):**
- **Expression:** Physical power, technology, brute force, material strength
- **Visual:** Muscular, technological, industrial, physical dominance
- **Themes:** Strength, technology, physical prowess, engineering
- **Combined with Lawful:** Roman legions, disciplined military tech
- **Combined with Chaotic:** Savage, Primal warriors, primal strength, Strength through adaptability
- **Combined with Pure:** [lawfuls]Noble knights, [chaotics/neutrals]honorable warriors, principled strength, [evils]Deathdealers
- **Combined with Corrupt:** Mercenaries, brutal enforcers, exploitative tech, savage tribalists
- **Combined with Good:** [spiritualists]Paladins, [neutrals]Conquerors, []
- **Combined with Evil:** Brawlers, Slayers, Manhunters, Cutthroats and Marauders, based on archetypes.

**Magic (+100):**
- **Expression:** Mystical power, psionics, divine, ethereal strength
- **Visual:** Ethereal, mystical, flowing, transcendent, otherworldly
- **Themes:** Mysticism, transcendence, spiritual power, arcane knowledge
- **Combined with Lawful:** Orderly magic, heals and buffs, Auras, structured rituals, formal spellcasting
- **Combined with Chaotic:** Wild magic, Random Mana, unpredictable spells, natural forces, summoning magic, shapeshiftings.
- **Combined with Pure:** Arcane, Raw Mana, pure power
- **Combined with Corrupt:** Dark Mana, Blight, Demonology, Blood magics, Necromancy, curses, exploitative spells, selfish power
- **Combined with Good:** Holy light magics, nature magics, high magics, cleansing and restorative, and also potential lethal
- **Combined with Evil:** Fel, ruin magics, malevolent curses and afflictions, soul magics.

**Balanced (0):**
- **Expression:** Hybrid approach, magitech, balanced power sources
- **Visual:** Integrated designs, balanced aesthetics
- **Themes:** Synergy, integration, balanced power

### Cooperative/Competitive Axis

**Cooperative (+100):**
- **Aesthetics:** Communistic, collective, shared, unified
- **Visual:** Shared spaces, collective ownership, uniform designs, communal structures
- **Themes:** Collective action, shared resources, mutual support, unity
- **Colors:** Uniform palettes, shared aesthetics, collective identity
- **Combined with Egalitarian:** True communism, complete equality, shared everything
- **Combined with Authoritarian:** Collective under leadership, structured cooperation

**Competitive (-100):**
- **Aesthetics:** Capitalistic, individualistic, competitive, stratified
- **Visual:** Individual spaces, private ownership, varied designs, competitive structures
- **Themes:** Individual achievement, private property, competition, meritocracy
- **Colors:** Varied palettes, individual expression, competitive distinction
- **Combined with Egalitarian:** Merit-based competition, equal opportunity
- **Combined with Authoritarian:** Oligarchic competition, power-based hierarchy

**Neutral (0):**
- **Aesthetics:** Balanced, mixed economy, selective cooperation
- **Visual:** Mixed designs, balanced ownership
- **Themes:** Pragmatic balance, selective competition

### Vengeful/Forgiving Axis

**Vengeful (-100):**
- **Aesthetics:** Edgy, sharp, angular, aggressive
- **Visual:** Sharp edges, angular designs, aggressive forms, cutting lines
- **Themes:** Retribution, grudges, sharp justice, cutting responses
- **Colors:** Dark, sharp contrasts, edgy palettes
- **Combined with Lawful:** Systematic vengeance, legal retribution, structured justice
- **Combined with Chaotic:** Wild vengeance, unpredictable revenge, chaotic retribution

**Forgiving (+100):**
- **Aesthetics:** Round, soft, curved, gentle
- **Visual:** Rounded edges, soft curves, gentle forms, flowing lines
- **Themes:** Mercy, reconciliation, soft justice, gentle responses
- **Colors:** Soft, gentle contrasts, rounded palettes
- **Combined with Lawful:** Structured forgiveness, legal mercy, formal reconciliation
- **Combined with Chaotic:** Spontaneous forgiveness, natural mercy, organic reconciliation

**Neutral (0):**
- **Aesthetics:** Balanced, pragmatic, context-dependent
- **Visual:** Moderate edges, balanced forms
- **Themes:** Situational justice, balanced responses

### Craven/Bold Axis

**Craven (-100):**
- **Aesthetics:** Light, subtle, muted, understated
- **Visual:** Subtle designs, muted forms, light structures, understated presence
- **Themes:** Caution, subtlety, avoidance, defensive
- **Colors:** Light tones, subtle contrasts, muted palettes, pastels
- **Combined with Lawful:** Cautious order, careful structure, defensive hierarchy
- **Combined with Chaotic:** Subtle chaos, hidden wildness, quiet unpredictability

**Bold (+100):**
- **Aesthetics:** Bold, stark, dramatic, prominent
- **Visual:** Stark designs, bold forms, dramatic structures, prominent presence
- **Themes:** Confidence, boldness, dramatic action, prominent presence
- **Colors:** Bold tones, stark contrasts, dramatic palettes, vibrant hues
- **Combined with Lawful:** Bold order, dramatic structure, prominent hierarchy
- **Combined with Chaotic:** Wild boldness, dramatic chaos, prominent unpredictability

**Neutral (0):**
- **Aesthetics:** Balanced, moderate, context-appropriate
- **Visual:** Moderate designs, balanced forms
- **Themes:** Situational boldness, balanced confidence

---

## Outlook Combination Archetypes

Entities can effectively have **up to 3 regular outlooks** OR **2 fanatic outlooks** (extreme positions)(flexible, no hardcoded). These combinations create distinct archetypical identities with unique names, aesthetics, and organizational structures. note that entities have all outlooks simultaneously, but only the 3 highest ones (or 2 highest if they cross a threshold) are counted for aggregation.

**Note:** The combinations below are descriptive examples only. Runtime does not encode explicit combo enums; games may label or visualize them however they choose.

### Fanatic Outlook Combinations

**Fanatic outlooks** are extreme, locked positions that create powerful archetypes with strong visual and thematic identities.

#### Fanatic Warlike Combinations

**Fanatic Warlike + Fanatic/Regular Xenophobic:**
- **Archetype:** "Xenocidal Purgers"
- **Aesthetics:** Brutal, puritan, defensive, isolationist military
- **Visual:** Fortified structures, defensive architecture, puritan military uniforms, closed borders
- **Themes:** Systematic elimination of outsiders, defensive isolation, puritan warfare
- **Organizations:**
  - **Bands:** "Purge Squads" - Small, elite units for eliminating threats
  - **Guilds:** "Purification Chapters" / "Templar Chapters" - Military orders dedicated to xenocide (Lawful variant uses "Templar Chapters", Chaotic uses "Blood Courts")
  - **Villages:** "Fortress Settlements" - Heavily fortified, closed communities
- **Naming:** Lawful = "Legion of Purity", Chaotic = "Blood Hounds", Neutral = "Purge Companies"
- **Special Note:** When combined with Spiritual outlook, these become "Xeno Crusaders" with "Templar Chapters" (Lawful/Good) or "Blood Courts" (Chaotic/Evil) as organizational structures

**Fanatic Warlike + Fanatic/Regular Spiritual:**
- **Archetype:** "Xeno Crusaders"
- **Aesthetics:** Elegant but brutal, templar, holy warriors, divine military
- **Visual:** Ornate armor with religious symbols, elegant but deadly weapons, templar architecture
- **Themes:** Holy warfare, divine mission, crusading zeal, spiritual combat
- **Organizations:**
  - **Bands:** "Crusader Bands" - Holy warrior groups
  - **Guilds:** "Templar Chapters" - Religious military orders (Lawful/Neutral), "Blood Courts" - Chaotic/Evil variant with ritualistic combat and blood ceremonies
  - **Villages:** "Crusader Forts" - Military-religious settlements
- **Naming:** Lawful = "Order of the Sacred Blade", Chaotic = "Wild Templars" / "Blood Courts", Pure = "Divine Crusaders", Evil = "Blood Courts" / "Dark Templars"

**Fanatic Warlike + Fanatic/Regular Materialist:**
- **Archetype:** "Mercenary Corporations"
- **Aesthetics:** Brutalist military-industrial, corporate warfare, profit-driven combat
- **Visual:** Industrial military gear, corporate logos on armor, brutalist fortifications
- **Themes:** War for profit, military-industrial complex, corporate conquest
- **Organizations:**
  - **Bands:** "Contract Squads" - Professional mercenary units
  - **Guilds:** "War Corporations" - Military-industrial organizations
  - **Villages:** "Mercenary Forts" - Corporate military settlements
- **Naming:** Lawful = "Legion Contractors", Chaotic = "Wild Mercs", Corrupt = "Blood Corporations"

#### Fanatic Spiritual Combinations

**Fanatic Spiritual + Fanatic/Regular Peaceful:**
- **Archetype:** "Docile Worshippers"
- **Aesthetics:** Peaceful elegant, contemplative, serene, transcendent
- **Visual:** Flowing robes, elegant architecture, peaceful gardens, serene spaces
- **Themes:** Devotion, contemplation, peaceful worship, spiritual harmony
- **Organizations:**
  - **Bands:** "Contemplative Circles" - Peaceful spiritual groups
  - **Guilds:** "Temple Orders" - Religious organizations focused on worship
  - **Villages:** "Sanctuary Settlements" - Peaceful religious communities
- **Naming:** Lawful = "Order of Serenity", Chaotic = "Wild Mystics", Pure = "Divine Contemplatives"

**Fanatic Spiritual + Fanatic/Regular Materialist:**
- **Archetype:** "Sacred Merchants"
- **Aesthetics:** Elegant commerce, spiritual trade, refined materialism
- **Visual:** Ornate marketplaces, elegant trade goods, refined commercial architecture
- **Themes:** Sacred commerce, spiritual value in material goods, refined trade
- **Organizations:**
  - **Bands:** "Trade Circles" - Spiritual merchant groups
  - **Guilds:** "Sacred Merchant Houses" - Religious commercial organizations
  - **Villages:** "Temple Markets" - Commercial-religious settlements
- **Naming:** Lawful = "Sacred Trade Order", Chaotic = "Mystic Traders", Pure = "Divine Merchants"

**Fanatic Spiritual + Fanatic/Regular Xenophobic:**
- **Archetype:** "Puritan Clergy"
- **Aesthetics:** Conservative elegant, traditional, insular, refined isolation
- **Visual:** Traditional religious architecture, conservative elegant clothing, closed communities
- **Themes:** Traditional faith, insular spirituality, refined isolation
- **Organizations:**
  - **Bands:** "Puritan Circles" - Traditionalist religious groups
  - **Guilds:** "Orthodox Chapters" - Traditional religious orders
  - **Villages:** "Puritan Settlements" - Traditionalist religious communities
- **Naming:** Lawful = "Orthodox Order", Chaotic = "Wild Puritans", Pure = "Divine Traditionalists"

#### Fanatic Materialist Combinations

**Fanatic Materialist + Fanatic/Regular Warlike:**
- **Archetype:** "War Industrialists"
- **Aesthetics:** Brutalist military-industrial, corporate warfare, profit-driven combat
- **Visual:** Industrial military gear, corporate warfare, brutalist fortifications
- **Themes:** War for profit, military-industrial complex, corporate conquest
- **Organizations:** (See Fanatic Warlike + Materialist above)

**Fanatic Materialist + Fanatic/Regular Spiritual:**
- **Archetype:** "Sacred Industrialists"
- **Aesthetics:** Elegant industrial, refined production, spiritual commerce
- **Visual:** Refined industrial architecture, elegant production facilities, spiritual commercial spaces
- **Themes:** Sacred production, spiritual value in industry, refined commerce
- **Organizations:**
  - **Bands:** "Production Circles" - Industrial-spiritual groups
  - **Guilds:** "Sacred Industries" - Religious-industrial organizations
  - **Villages:** "Temple Factories" - Industrial-religious settlements
- **Naming:** Lawful = "Sacred Industry Order", Chaotic = "Wild Producers", Pure = "Divine Industrialists"

#### Fanatic Peaceful Combinations

**Fanatic Peaceful + Fanatic/Regular Spiritual:**
- **Archetype:** "Contemplative Mystics"
- **Aesthetics:** Peaceful elegant, serene, transcendent, contemplative
- **Visual:** Flowing robes, elegant peaceful architecture, serene gardens, contemplative spaces
- **Themes:** Peaceful devotion, contemplation, spiritual harmony, non-violence
- **Organizations:**
  - **Bands:** "Mystic Circles" - Peaceful contemplative groups
  - **Guilds:** "Contemplative Orders" - Peaceful religious organizations
  - **Villages:** "Mystic Sanctuaries" - Peaceful contemplative settlements
- **Naming:** Lawful = "Order of Contemplation", Chaotic = "Wild Mystics", Pure = "Divine Contemplatives"

### Regular Outlook Combinations (3 Outlooks)

When entities have **3 regular outlooks**, the combination creates more nuanced archetypes that blend multiple cultural expressions.

#### Example: Warlike + Xenophobic + Authoritarian
- **Archetype:** "Fortress Defenders"
- **Aesthetics:** Defensive military hierarchy, closed structured warfare
- **Visual:** Fortified hierarchical structures, defensive military uniforms, closed communities
- **Themes:** Defensive warfare, hierarchical isolation, structured defense
- **Organizations:**
  - **Bands:** "Defense Squads" - Hierarchical defensive units
  - **Guilds:** "Fortress Orders" - Military-hierarchical organizations
  - **Villages:** "Fortress Settlements" - Defensive hierarchical communities
- **Naming:** Lawful = "Legion of Defense", Chaotic = "Wild Fortresses", Neutral = "Defense Companies"

#### Example: Peaceful + Egalitarian + Spiritual
- **Archetype:** "Harmony Circles"
- **Aesthetics:** Peaceful egalitarian elegance, shared spiritual spaces
- **Visual:** Open egalitarian architecture, flowing peaceful designs, shared spiritual spaces
- **Themes:** Peaceful equality, shared spirituality, harmonious communities
- **Organizations:**
  - **Bands:** "Harmony Circles" - Peaceful egalitarian groups
  - **Guilds:** "Egalitarian Temples" - Peaceful spiritual organizations
  - **Villages:** "Harmony Settlements" - Peaceful egalitarian communities
- **Naming:** Lawful = "Order of Harmony", Chaotic = "Wild Circles", Pure = "Divine Harmony"

#### Example: Materialist + Competitive + Authoritarian
- **Archetype:** "Merchant Oligarchs"
- **Aesthetics:** Brutalist competitive hierarchy, corporate oligarchy
- **Visual:** Industrial hierarchical structures, competitive commercial spaces, oligarchic architecture
- **Themes:** Competitive commerce, hierarchical trade, oligarchic materialism
- **Organizations:**
  - **Bands:** "Trade Squads" - Competitive merchant groups
  - **Guilds:** "Merchant Houses" - Oligarchic commercial organizations
  - **Villages:** "Merchant Cities" - Competitive hierarchical settlements
- **Naming:** Lawful = "Merchant Order", Chaotic = "Wild Traders", Corrupt = "Oligarch Companies"

### Special Combination Nuances

Each outlook combination creates unique cultural expressions and social behaviors that emerge organically from the interaction of values. These nuances affect how entities interact, socialize, conduct business, and express their culture.

#### Spiritual + Materialist Combinations

**Spiritual Materialists (Sacred Commerce):**
- **Business in Synagogue/Temple:** Commerce conducted in sacred spaces, combining trade with worship
- **Boosted Relations:** Business deals in sacred spaces increase relation bonuses (+15 instead of +10)
- **Increased Agreement Chances:** Sacred commerce creates trust, +20% success rate on trade agreements
- **Blessed Contracts:** Agreements made in temples are considered sacred, harder to break
- **Ritual Trade:** Ceremonial exchange of goods, spiritual value attached to commerce
- **Cultural Expression:** Merchants are also clergy, commerce is worship, wealth is divine blessing

**Pure Spiritual Materialists:**
- **Fair Trade Worship:** Commerce as service to community, fair prices, sustainable practices
- **Blessed Markets:** Markets are sacred spaces, all transactions are blessed
- **Divine Commerce:** Trade as expression of divine will, prosperity as spiritual reward

**Corrupt Spiritual Materialists:**
- **Exploitative Worship:** Use religion to justify unfair prices, manipulate through faith
- **Sacred Scams:** Religious authority used to enforce bad deals
- **Blessed Exploitation:** Commerce that exploits, justified by religious doctrine

#### Warlike + Xenophilic Combinations

**Xenophilic Warlikes (Warrior Exchange):**
- **Share Battle Methods:** Exchange combat techniques, tactics, and strategies with other cultures
- **Tournaments & Arena Fights:** Organized combat events for socializing, entertainment, and betting
- **Cultural Combat Exchange:** Learn fighting styles from other cultures, adapt techniques
- **Arena Socializing:** Combat as social activity, tournaments as community events
- **Betting on Fights:** Economic activity around arena events, gambling on outcomes
- **Honor-Based Combat:** Respectful competition, learning from opponents
- **Cross-Cultural Training:** Train with warriors from other cultures, exchange knowledge

**Pure Xenophilic Warlikes:**
- **Friendly Tournaments:** Respectful competition, cultural exchange through combat
- **Teaching Battles:** Share techniques freely, help others improve
- **Honor Duels:** Structured, respectful combat for settling disputes

**Corrupt Xenophilic Warlikes:**
- **Exploitative Tournaments:** Use combat events to exploit others, rigged matches
- **Betting Manipulation:** Fix fights for profit, exploit gambling
- **Stolen Techniques:** Learn from others but don't share, hoard knowledge

#### Warlike + Xenophobic Combinations

**Xenophobic Warlikes (Purge Warriors):**
- **Arena Fights Against Xenos:** Combat events featuring enslaved or captured outsiders
- **Supremacy Demonstrations:** Arena fights to show dominance over other races/cultures
- **Xenocide Training:** Use captured outsiders for combat practice, desensitization
- **Internal Tournaments:** Combat events only for members, exclusion of outsiders
- **Purge Rituals:** Combat as purification, eliminating threats through violence
- **Defensive Isolation:** Combat training focused on repelling outsiders

**Corrupt/Chaotic Xenophobic Warlikes:**
- **Xeno Gladiator Fights:** Enslaved outsiders forced to fight for entertainment
- **Brutal Supremacy:** Extreme violence to demonstrate superiority
- **Ritual Sacrifice Combat:** Killing outsiders in arena as religious/purification ritual
- **No Honor:** Treat outsiders as subhuman, no rules in combat

**Pure/Lawful Xenophobic Warlikes:**
- **Defensive Tournaments:** Combat training for protection, not entertainment
- **Structured Purification:** Organized, lawful elimination of threats
- **Honor in Defense:** Fight with principles, even against outsiders

#### Peaceful + Spiritual Combinations

**Peaceful Spiritualists (Contemplative Harmony):**
- **Meditation Circles:** Group contemplation, shared spiritual practice
- **Healing Rituals:** Sacred ceremonies focused on restoration and wellness
- **Peace Prayers:** Religious practices promoting harmony and non-violence
- **Temple Gardens:** Sacred spaces for peaceful reflection
- **Harmony Ceremonies:** Rituals that bring communities together

**Pure Peaceful Spiritualists:**
- **Selfless Healing:** Sacrifice personal resources to heal others
- **Universal Peace:** Promote harmony for all, not just own community
- **Divine Compassion:** Religious practices focused on helping others

#### Materialist + Cooperative Combinations

**Cooperative Materialists (Collective Commerce):**
- **Shared Workshops:** Communal production facilities, collective ownership
- **Resource Pools:** Combined materials, distributed based on need
- **Collective Markets:** Shared commercial spaces, group bargaining
- **Mutual Investment:** Community funds for collective projects
- **Cooperative Production:** Work together to maximize efficiency

**Pure Cooperative Materialists:**
- **Fair Distribution:** Equal sharing of resources and profits
- **Community Wealth:** Prosperity benefits all members
- **Sustainable Practices:** Long-term thinking, collective benefit

**Corrupt Cooperative Materialists:**
- **Exploitative Collectives:** Use cooperation to exploit outsiders
- **Internal Hoarding:** Share within group, hoard from others
- **Manipulative Cooperation:** Fake cooperation to gain advantage

#### Warlike + Peaceful Combinations

**Warlike Peaceful (Defensive Harmony):**
- **Defensive Combat:** Fight only when necessary, prefer peace
- **Peaceful Warriors:** Skilled fighters who avoid conflict
- **Mediation Through Strength:** Use military capability to enforce peace
- **Defensive Alliances:** Military cooperation for mutual protection
- **Combat as Last Resort:** Exhaust all peaceful options first

**Pure Warlike Peaceful:**
- **Noble Defense:** Fight with honor, only to protect
- **Peace Through Strength:** Maintain peace by being strong enough to deter
- **Protective Combat:** Fight to defend others, not for conquest

**Corrupt Warlike Peaceful:**
- **False Peace:** Appear peaceful while preparing for war
- **Passive-Aggressive Warfare:** Subtle attacks, deny aggression
- **Manipulative Defense:** Use defensive posture to justify aggression

#### Authoritarian + Egalitarian Combinations

**Authoritarian Egalitarians (Structured Equality):**
- **Meritocratic Hierarchy:** Equal opportunity, but clear ranks based on achievement
- **Ordered Democracy:** Structured collective decision-making
- **Elite Service:** Leaders serve the community, not exploit it
- **Formal Equality:** Legal equality, but social structure remains

**Pure Authoritarian Egalitarians:**
- **Noble Leadership:** Leaders chosen by merit, serve community
- **Structured Fairness:** Clear rules ensure equal treatment
- **Service Hierarchy:** Higher ranks mean more responsibility, not privilege

#### Competitive + Cooperative Combinations

**Competitive Cooperatives (Team Competition):**
- **Group vs Group:** Teams compete, but internally cooperative
- **Collective Achievement:** Success benefits entire team
- **Internal Cooperation, External Competition:** Work together to beat others
- **Team Tournaments:** Competitive events between cooperative groups

**Pure Competitive Cooperatives:**
- **Fair Team Competition:** Respectful rivalry, honor in competition
- **Shared Victory:** Team success benefits all members equally
- **Cooperative Excellence:** Best performance through teamwork

**Corrupt Competitive Cooperatives:**
- **Exploitative Teams:** Use cooperation to exploit other teams
- **Internal Betrayal:** Compete within team while appearing cooperative
- **Rigged Competition:** Cheat to ensure team victory

#### Spiritual + Warlike Combinations

**Spiritual Warlikes (Holy Warriors):**
- **Sacred Combat:** Warfare as religious duty, divine mission
- **Blessed Weapons:** Ritual preparation of arms, consecrated equipment
- **Prayer Before Battle:** Religious ceremonies before combat
- **Divine Victory:** Success in battle as proof of divine favor
- **Templar Training:** Religious military education, spiritual warriors

**Pure Spiritual Warlikes:**
- **Righteous Combat:** Fight for just causes, with honor
- **Divine Protection:** Faith provides strength, protection in battle
- **Sacred Duty:** Warfare as service to higher purpose

**Corrupt Spiritual Warlikes:**
- **False Holy War:** Use religion to justify any conflict
- **Dark Blessings:** Curses and dark magic in combat
- **Exploitative Faith:** Use religious authority to force combat service

#### Materialist + Warlike Combinations

**Materialist Warlikes (War Economy):**
- **Mercenary Service:** Fight for profit, professional soldiers
- **War Industries:** Production focused on military goods
- **Combat Contracts:** Business agreements for military service
- **Weapon Trade:** Commerce in arms and military equipment
- **War Profiteering:** Profit from conflicts, economic warfare

**Pure Materialist Warlikes:**
- **Fair Mercenary Service:** Honorable contracts, fair payment
- **Defensive Commerce:** Trade to support defensive capabilities
- **Sustainable War Economy:** Long-term thinking, not just profit

**Corrupt Materialist Warlikes:**
- **Ruthless Mercenaries:** Fight for highest bidder, no honor
- **Exploitative War Trade:** Unfair prices, profiteering from conflict
- **Slavery for War:** Enslave others to fight, maximize profit

#### Xenophobic + Spiritual Combinations

**Xenophobic Spiritualists (Orthodox Isolation):**
- **Sacred Purity:** Religious practices focused on maintaining cultural purity
- **Heretic Purges:** Eliminate those who deviate from orthodoxy
- **Traditional Rituals:** Preserve ancient practices, resist change
- **Sacred Isolation:** Keep faith pure from outside influence
- **Inquisitorial Practices:** Root out foreign religious influence

**Pure Xenophobic Spiritualists:**
- **Defensive Faith:** Protect traditional beliefs, not attack others
- **Sacred Preservation:** Maintain orthodoxy through isolation
- **Honorable Exclusion:** Respectful but firm boundaries

**Corrupt Xenophobic Spiritualists:**
- **Violent Purity:** Force orthodoxy through violence
- **Religious Supremacy:** Use faith to justify xenophobia
- **Dark Orthodoxy:** Twisted traditional practices, corrupted purity

#### Xenophilic + Spiritual Combinations

**Xenophilic Spiritualists (Cosmopolitan Faith):**
- **Multicultural Worship:** Blend religious practices from different cultures
- **Sacred Exchange:** Share religious knowledge, learn from others
- **Universal Faith:** Spiritual practices that welcome all
- **Interfaith Dialogue:** Religious discussions across cultures
- **Diverse Rituals:** Incorporate elements from multiple traditions

**Pure Xenophilic Spiritualists:**
- **Universal Harmony:** Faith that unites, not divides
- **Sacred Diversity:** Spiritual value in cultural exchange
- **Inclusive Worship:** Welcome all to participate

**Corrupt Xenophilic Spiritualists:**
- **Exploitative Conversion:** Use religion to manipulate others
- **False Inclusivity:** Appear welcoming but exploit newcomers
- **Cultural Appropriation:** Steal religious practices for power

#### Egalitarian + Warlike Combinations

**Egalitarian Warlikes (Democratic Warriors):**
- **Citizen Soldiers:** All members participate in defense
- **Equal Combat Training:** Everyone learns to fight
- **Democratic Military:** Collective decision-making in warfare
- **Shared Defense:** Equal responsibility for protection
- **Merit-Based Ranks:** Advancement through skill, not birth

**Pure Egalitarian Warlikes:**
- **Defensive Democracy:** Fight to protect equality
- **Equal Participation:** All contribute to defense equally
- **Honorable Equality:** Combat with principles, equal treatment

**Corrupt Egalitarian Warlikes:**
- **False Equality:** Claim equality while maintaining hierarchy
- **Exploitative Defense:** Use defense to justify inequality
- **Manipulative Democracy:** Appear equal while controlling

#### Authoritarian + Warlike Combinations

**Authoritarian Warlikes (Feudal Military):**
- **Knightly Hierarchy:** Strict military ranks, feudal structure
- **Honor Codes:** Formal rules of combat, chivalry
- **Elite Warriors:** Best fighters hold highest ranks
- **Structured Warfare:** Organized, disciplined military
- **Service Obligation:** Mandatory military service

**Pure Authoritarian Warlikes:**
- **Noble Knights:** Honor-bound warriors, principled combat
- **Service Leadership:** Leaders serve, not exploit
- **Defensive Hierarchy:** Structure for protection, not oppression

**Corrupt Authoritarian Warlikes:**
- **Tyrannical Military:** Use force to maintain power
- **Exploitative Service:** Force others to fight for personal gain
- **Ruthless Hierarchy:** Power through violence, no honor

#### Peaceful + Materialist Combinations

**Peaceful Materialists (Sustainable Commerce):**
- **Fair Trade:** Commerce that benefits all parties
- **Sustainable Production:** Long-term thinking, environmental care
- **Diplomatic Commerce:** Trade as peace-building tool
- **Cooperative Markets:** Shared commercial spaces, mutual benefit
- **Resource Conservation:** Careful use, avoid waste

**Pure Peaceful Materialists:**
- **Altruistic Commerce:** Trade to help others, not just profit
- **Sustainable Prosperity:** Wealth that doesn't harm
- **Peaceful Prosperity:** Economic growth through cooperation

**Corrupt Peaceful Materialists:**
- **False Peace:** Appear peaceful while exploiting
- **Passive Exploitation:** Subtle economic manipulation
- **Manipulative Trade:** Use commerce to control others

---

## Hive Mind Entities

Some entities are **hive-minded**, sharing consciousness to varying degrees. These entities are more attuned to their collective aggregate entity and may share outlooks, alignments, and behavior profiles across the collective.

### Hive Mind Characteristics

**Shared Consciousness:**
- Entities within a hive mind share awareness, experiences, and reactions
- Affected by the same stimuli, events, and conditions
- Collective decision-making, shared knowledge pool
- Unified response to external forces

**Collective Attunement:**
- Individual drones are more connected to aggregate entity than typical members
- Aggregate entity's alignment/outlook strongly influences individual drones
- Shared emotional states, synchronized reactions
- Collective memory and experience

**Shared Profiles:**
- Hive minds may have **shared outlook/alignment/behavior profiles** at the aggregate level
- Individual drones inherit or synchronize with collective profile
- Profile changes at aggregate level affect all drones
- Unified cultural expression across the hive

### Drone Deviation

**Natural Deviation:**
- Over time, some drones may deviate from the main hive mind
- Develop different outlooks, alignments, or behaviors
- Caused by isolation, external influence, mutation, or individual experiences
- Deviation strength varies (minor drift vs complete separation)

**Deviation Mechanics:**
- **Deviation Rate:** How quickly drones drift from collective profile
- **Deviation Resistance:** How strongly hive mind maintains unity
- **Deviation Threshold:** Point at which drone is considered "deviant"
- **Deviation Severity:** Degree of difference from collective profile

### Hive Mind Response to Deviation

Different hive minds handle deviant drones based on their **alignment, outlook, and behavior profiles**:

#### Purge Responses (Authoritarian, Xenophobic, Lawful variants)

**Authoritarian Hive Minds:**
- **Systematic Purge:** Eliminate deviants to maintain unity
- **Structured Elimination:** Formal process for identifying and removing deviants
- **Preventive Measures:** Strict monitoring, early intervention
- **Example:** Lawful Authoritarian Xenophobic = "Purification Protocols" - systematic elimination of deviants

**Xenophobic Hive Minds:**
- **Purity Purge:** Remove deviants to maintain cultural/ideological purity
- **Isolation First:** Isolate deviants before elimination
- **Contamination Prevention:** Prevent deviant ideas from spreading
- **Example:** Pure Xenophobic = "Purity Enforcement" - remove deviants to protect collective purity

**Lawful Hive Minds:**
- **Orderly Purge:** Structured, rule-based elimination of deviants
- **Legal Framework:** Formal rules for deviation and punishment
- **Systematic Process:** Step-by-step identification and removal
- **Example:** Lawful Good = "Rehabilitation or Removal" - attempt correction, then eliminate if failed

#### Plurality Responses (Egalitarian, Xenophilic, Peaceful variants)

**Egalitarian Hive Minds:**
- **Allow Plurality:** Accept diverse outlooks/alignments within collective
- **Democratic Expression:** Deviants can voice different perspectives
- **Collective Decision:** Vote or consensus on whether to accept deviation
- **Example:** Egalitarian Peaceful = "Harmony Through Diversity" - accept deviants as enriching collective

**Xenophilic Hive Minds:**
- **Embrace Deviation:** See deviants as valuable diversity
- **Cultural Enrichment:** Deviant perspectives add to collective knowledge
- **Integration:** Incorporate deviant ideas into collective understanding
- **Example:** Xenophilic Spiritual = "Sacred Diversity" - deviants bring new spiritual perspectives

**Peaceful Hive Minds:**
- **Tolerant Plurality:** Accept deviants without conflict
- **Mediation:** Help deviants integrate or find their place
- **Non-Violent Resolution:** Avoid elimination, seek understanding
- **Example:** Peaceful Good = "Compassionate Integration" - help deviants find harmony with collective

#### Violence-Based Responses (Warlike, Chaotic, Competitive variants)

**Warlike Hive Minds:**
- **Combat Resolution:** Stronger drone decides through combat
- **Arena Trials:** Deviants fight for right to exist or change collective
- **Might Makes Right:** Physical strength determines which profile dominates
- **Example:** Warlike Chaotic = "Combat Democracy" - deviants fight to change collective profile

**Chaotic Hive Minds:**
- **Unpredictable Response:** Random or chaotic handling of deviants
- **Survival of Fittest:** Natural selection, strongest/most adaptable wins
- **Organic Evolution:** Collective profile evolves through conflict
- **Example:** Chaotic Evil = "Survival of Strongest" - deviants compete, winner changes collective

**Competitive Hive Minds:**
- **Competitive Resolution:** Deviants compete for influence
- **Marketplace of Ideas:** Best ideas win through competition
- **Merit-Based Change:** Successful deviants shift collective profile
- **Example:** Competitive Materialist = "Ideological Competition" - deviants compete, successful ones change collective

#### Hybrid Responses (Complex Combinations)

**Authoritarian + Egalitarian:**
- **Meritocratic Plurality:** Allow deviation if deviant proves superior
- **Structured Competition:** Formal process for deviants to prove value
- **Elite Integration:** Successful deviants join leadership, change collective

**Warlike + Peaceful:**
- **Defensive Plurality:** Accept deviants but prepare for conflict
- **Mediated Combat:** Resolve deviation through controlled combat
- **Peaceful Strength:** Use strength to protect diversity

**Spiritual + Materialist:**
- **Sacred Commerce:** Deviants can "buy" right to exist through contribution
- **Ritual Resolution:** Sacred ceremonies determine deviant fate
- **Divine Market:** Spiritual value determines deviant acceptance

### Hive Mind Profile Examples

**Lawful Good Pure Authoritarian Xenophobic Hive Mind:**
- **Profile:** "Ordered Purity Collective"
- **Deviation Response:** Systematic purge, structured elimination
- **Expression:** "Deviants are threats to collective purity. They must be identified, isolated, and removed through proper channels."
- **Visual:** Uniform, structured, clean, organized

**Chaotic Evil Corrupt Warlike Competitive Hive Mind:**
- **Profile:** "Survival of Strongest"
- **Deviation Response:** Combat resolution, stronger drone decides
- **Expression:** "Deviants challenge the collective? Let them fight. The strongest wins, the collective adapts."
- **Visual:** Varied, competitive, brutal, dynamic

**Neutral Good Egalitarian Xenophilic Peaceful Hive Mind:**
- **Profile:** "Harmonious Diversity Collective"
- **Deviation Response:** Allow plurality, embrace diversity
- **Expression:** "Deviants bring new perspectives. We welcome diversity and learn from all voices."
- **Visual:** Diverse, harmonious, integrated, welcoming

**Lawful Neutral Spiritual Materialist Hive Mind:**
- **Profile:** "Sacred Commerce Collective"
- **Deviation Response:** Ritual resolution, contribution-based acceptance
- **Expression:** "Deviants may exist if they contribute value. Sacred ceremonies determine their place."
- **Visual:** Ornate, commercial, ritualistic, structured

### Drone Deviation Mechanics

**Deviation Triggers:**
- **Isolation:** Drone separated from collective for extended period
- **External Influence:** Contact with different cultures/entities
- **Individual Experience:** Unique events that affect only one drone
- **Mutation:** Biological or psychological changes
- **Resistance:** Natural resistance to collective influence

**Deviation Progression:**
1. **Minor Drift:** Slight differences, still mostly aligned
2. **Moderate Deviation:** Noticeable differences, some conflict
3. **Major Deviation:** Significant differences, strong conflict
4. **Complete Separation:** Drone breaks from hive mind entirely

**Deviation Detection:**
- **Collective Awareness:** Hive mind senses deviation automatically
- **Monitoring Systems:** Active surveillance for deviant behavior
- **Reporting Mechanisms:** Other drones report deviant activity
- **Ritual Detection:** Sacred ceremonies identify deviants

**Deviation Response Timeline:**
- **Immediate:** Some hive minds respond instantly to deviation
- **Gradual:** Others allow deviation to develop before responding
- **Periodic:** Some check for deviation at regular intervals
- **Event-Driven:** Response triggered by specific events

### Hive Mind Visual Expression

**Unified Hive Minds (High Unity):**
- **Visual:** Uniform appearance, synchronized movements, collective aesthetics
- **Colors:** Shared palette, unified themes
- **Structures:** Collective architecture, shared spaces
- **Behavior:** Synchronized actions, coordinated responses

**Pluralistic Hive Minds (Allowed Deviation):**
- **Visual:** Diverse appearance, varied movements, mixed aesthetics
- **Colors:** Blended palettes, integrated themes
- **Structures:** Varied architecture, shared but diverse spaces
- **Behavior:** Coordinated but varied actions, diverse responses

**Combat-Based Hive Minds:**
- **Visual:** Competitive appearance, dynamic movements, arena aesthetics
- **Colors:** Contrasting palettes, competitive themes
- **Structures:** Arena spaces, combat zones, competitive architecture
- **Behavior:** Competitive actions, conflict-driven responses

### Cross-Game Applications

**Godgame:**
- **Hive Villages:** Villages with shared consciousness
- **Deviant Villagers:** Individual villagers who break from collective
- **Hive Gods:** Deities with hive-minded followers
- **Collective Miracles:** Miracles that affect entire hive simultaneously

**Space4X:**
- **Hive Colonies:** Colonies with shared consciousness
- **Deviant Crew:** Individual crew members who deviate
- **Hive Factions:** Factions with unified consciousness
- **Collective Decisions:** Group decisions made as one

**Shared Systems:**
- **Hive Aggregates:** Aggregate entities with hive mind characteristics
- **Deviation Tracking:** Systems that monitor and respond to deviation
- **Collective Profiles:** Shared alignment/outlook/behavior at aggregate level
- **Response Systems:** Mechanisms for handling deviant drones

---

## Policies & Edicts

Each outlook combination archetype has associated **policies** (ongoing rules) and **edicts** (specific decrees/actions) that reflect their cultural values and priorities. These are presentation-layer expressions; PureDOTS provides only the semantic signals (alignment/outlook values), and games interpret these into specific policy implementations.

### Materialist Policies & Edicts

**Core Materialist Policies:**
- **Child Labor:** Children work alongside adults (materialist efficiency, early skill development)
- **Industrial Efficiency:** Optimize production, minimize waste, maximize output
- **Resource Exploitation:** Extract maximum value from all resources
- **Construction Practices:** Advanced building techniques, better materials, structural engineering
- **Trade Networks:** Extensive commercial connections, market optimization
- **Wealth Accumulation:** Policies that favor capital growth and material prosperity

**Materialist Edicts:**
- "Expand Production" - Increase manufacturing capacity
- "Optimize Resources" - Improve resource extraction efficiency
- "Build Infrastructure" - Construct roads, warehouses, trade posts
- "Establish Markets" - Create commercial hubs
- "Industrialize" - Upgrade to industrial production methods

**Combined with Warlike:**
- **War Economy:** Production focused on military goods
- **Mercenary Contracts:** Hire professional soldiers for profit
- **War Profiteering:** Profit from conflicts

**Combined with Corrupt:**
- **Slavery:** Enslave workers for maximum profit
- **Exploitative Contracts:** Unfair labor agreements
- **Resource Hoarding:** Stockpile resources, create artificial scarcity

**Combined with Authoritarian:**
- **Forced Labor:** Compulsory work assignments
- **Production Quotas:** Mandatory output requirements
- **Industrial Hierarchy:** Strict workplace structure

### Warlike Policies & Edicts

**Core Warlike Policies:**
- **Child Warriors:** Children trained in combat from early age
- **Military Service:** Mandatory service, universal conscription
- **Combat Training:** Regular drills, martial education
- **Defensive Fortifications:** Strong walls, watchtowers, military infrastructure
- **Weapon Production:** Prioritize arms manufacturing
- **Veteran Benefits:** Rewards for military service

**Warlike Edicts:**
- "Mobilize Forces" - Prepare for combat
- "Fortify Borders" - Strengthen defensive positions
- "Train Militia" - Organize civilian defense
- "Arm the Population" - Distribute weapons
- "Honor Veterans" - Recognize military service

**Combined with Xenophobic:**
- **Purge Outsiders:** Expel or eliminate non-members
- **Defensive Isolation:** Fortify against external threats
- **Internal Security:** Monitor for infiltrators

**Combined with Spiritual:**
- **Holy War:** Religious justification for combat
- **Divine Mandate:** Warfare as sacred duty
- **Crusader Training:** Religious military education

**Combined with Corrupt:**
- **Mercenary Service:** Fight for highest bidder
- **Ruthless Tactics:** No honor, any means necessary
- **War Crimes:** Acceptable atrocities for victory

### Spiritual Policies & Edicts

**Core Spiritual Policies:**
- **Centralized Worship:** Unified religious practices, temple hierarchy
- **Ritual Observance:** Regular ceremonies, holy days, religious calendar
- **Divine Education:** Religious schooling, theological training
- **Temple Construction:** Build and maintain sacred spaces
- **Religious Hierarchy:** Clergy structure, spiritual authority
- **Faith-Based Governance:** Religious law, divine guidance

**Spiritual Edicts:**
- "Build Temple" - Construct religious structures
- "Observe Holy Day" - Mandatory religious observance
- "Appoint Clergy" - Establish religious leadership
- "Conduct Ritual" - Perform sacred ceremonies
- "Spread Faith" - Evangelize, convert others

**Combined with Peaceful:**
- **Contemplative Practices:** Meditation, peaceful worship
- **Non-Violence:** Pacifist principles
- **Harmony Rituals:** Ceremonies promoting peace

**Combined with Corrupt:**
- **Child Sacrifices:** Ritualistic killing for power (extreme corrupt spiritual)
- **Dark Rituals:** Forbidden ceremonies, blood magic
- **Religious Exploitation:** Use faith for personal gain
- **False Prophets:** Manipulative religious leaders

**Combined with Xenophobic:**
- **Religious Purity:** Exclude heretics, maintain orthodoxy
- **Sacred Isolation:** Keep faith pure from outside influence
- **Inquisitorial Practices:** Root out heresy

### Peaceful Policies & Edicts

**Core Peaceful Policies:**
- **Diplomatic Relations:** Maintain peace through negotiation
- **Non-Aggression:** Avoid conflict, seek resolution
- **Healing Practices:** Medical care, wellness programs
- **Cultural Exchange:** Share knowledge, promote understanding
- **Conflict Resolution:** Mediation, arbitration systems
- **Refugee Acceptance:** Welcome displaced populations

**Peaceful Edicts:**
- "Negotiate Peace" - Seek diplomatic solutions
- "Provide Aid" - Humanitarian assistance
- "Mediate Dispute" - Resolve conflicts peacefully
- "Cultural Festival" - Promote harmony through celebration
- "Refugee Program" - Accept and integrate displaced people

**Combined with Spiritual:**
- **Pacifist Worship:** Non-violent religious practices
- **Healing Rituals:** Sacred medical ceremonies
- **Peace Prayers:** Religious peacekeeping

**Combined with Egalitarian:**
- **Universal Rights:** Equal treatment for all
- **Democratic Peace:** Collective decision-making
- **Shared Resources:** Communal ownership

### Xenophobic Policies & Edicts

**Core Xenophobic Policies:**
- **Closed Borders:** Restrict entry, control immigration
- **Cultural Purity:** Maintain traditional values, resist change
- **Internal Surveillance:** Monitor for foreign influence
- **Traditional Practices:** Preserve customs, reject innovation
- **Isolationist Trade:** Limited external commerce
- **Defensive Posture:** Prepare for external threats

**Xenophobic Edicts:**
- "Close Borders" - Restrict access
- "Expel Outsiders" - Remove non-members
- "Preserve Traditions" - Maintain cultural purity
- "Monitor Infiltration" - Watch for foreign agents
- "Defensive Measures" - Prepare for external threats

**Combined with Warlike:**
- **Purge Campaigns:** Systematic elimination of outsiders
- **Defensive Warfare:** Fight to preserve isolation
- **Internal Security Forces:** Military dedicated to purity

### Xenophilic Policies & Edicts

**Core Xenophilic Policies:**
- **Open Borders:** Welcome outsiders, encourage immigration
- **Cultural Integration:** Blend traditions, embrace diversity
- **Diplomatic Outreach:** Active foreign relations
- **Artistic Exchange:** Share cultural expressions
- **Multicultural Education:** Teach diverse perspectives
- **Cosmopolitan Markets:** International trade hubs

**Xenophilic Edicts:**
- "Open Borders" - Welcome newcomers
- "Cultural Festival" - Celebrate diversity
- "Diplomatic Mission" - Establish foreign relations
- "Artistic Exchange" - Share cultural works
- "Integration Program" - Help newcomers adapt

**Combined with Egalitarian:**
- **Universal Welcome:** Equal treatment for all cultures
- **Multicultural Democracy:** Diverse representation
- **Shared Cultural Spaces:** Mixed community areas

### Authoritarian Policies & Edicts

**Core Authoritarian Policies:**
- **Strict Hierarchy:** Clear ranks, rigid structure
- **Mandatory Service:** Compulsory duties, conscription
- **Centralized Authority:** Single leadership, top-down control
- **Honor Codes:** Formal rules, strict discipline
- **Elite Privileges:** Benefits for leadership class
- **Obedience Training:** Education in following orders

**Authoritarian Edicts:**
- "Establish Hierarchy" - Create formal ranks
- "Mandatory Service" - Require participation
- "Enforce Discipline" - Punish disobedience
- "Grant Privileges" - Reward loyalty
- "Centralize Power" - Consolidate authority

**Combined with Warlike:**
- **Military Hierarchy:** Strict chain of command
- **Knightly Orders:** Honor-based military structure
- **Feudal Service:** Obligatory military duty

### Egalitarian Policies & Edicts

**Core Egalitarian Policies:**
- **Equal Rights:** Universal protections, no discrimination
- **Democratic Participation:** Collective decision-making
- **Shared Resources:** Communal ownership, equal access
- **Horizontal Structure:** Flat organization, no hierarchy
- **Mutual Aid:** Community support systems
- **Fair Distribution:** Equal allocation of resources

**Egalitarian Edicts:**
- "Establish Democracy" - Create collective governance
- "Share Resources" - Distribute equally
- "Equal Rights" - Guarantee protections
- "Community Assembly" - Collective decision-making
- "Mutual Aid Network" - Support systems

**Combined with Peaceful:**
- **Pacifist Democracy:** Non-violent collective governance
- **Harmony Councils:** Peaceful decision-making
- **Shared Peacekeeping:** Community-based conflict resolution

### Competitive Policies & Edicts

**Core Competitive Policies:**
- **Free Market:** Unrestricted commerce, competition
- **Meritocracy:** Rewards based on achievement
- **Private Property:** Individual ownership rights
- **Market Competition:** Encourage business rivalry
- **Entrepreneurship:** Support individual enterprise
- **Wealth Disparity:** Acceptable economic inequality

**Competitive Edicts:**
- "Establish Free Market" - Remove trade restrictions
- "Privatize Resources" - Transfer to private ownership
- "Encourage Competition" - Promote business rivalry
- "Reward Merit" - Recognize achievement
- "Market Deregulation" - Reduce government control

**Combined with Materialist:**
- **Capitalist Economy:** Profit-driven markets
- **Industrial Competition:** Competing manufacturers
- **Trade Wars:** Economic conflicts

**Combined with Corrupt:**
- **Exploitative Markets:** Unfair competition, monopolies
- **Wealth Hoarding:** Extreme inequality
- **Predatory Practices:** Ruthless business tactics

### Cooperative Policies & Edicts

**Core Cooperative Policies:**
- **Collective Ownership:** Shared resources, communal property
- **Mutual Support:** Community aid, shared responsibilities
- **Unified Goals:** Common objectives, collective action
- **Resource Sharing:** Pool materials, distribute equally
- **Collective Decision-Making:** Group choices, consensus
- **Shared Benefits:** Equal rewards, common prosperity

**Cooperative Edicts:**
- "Establish Collective" - Create shared ownership
- "Pool Resources" - Combine materials
- "Collective Action" - Organize group efforts
- "Share Benefits" - Distribute rewards equally
- "Mutual Aid" - Support community members

**Combined with Egalitarian:**
- **True Communism:** Complete equality, shared everything
- **Collective Democracy:** Group governance
- **Communal Living:** Shared spaces, resources

**Combined with Authoritarian:**
- **Structured Cooperation:** Organized under leadership
- **Collective Service:** Mandatory participation
- **Unified Command:** Centralized coordination

### Outlook Combination Policy Examples

**Fanatic Warlike + Xenophobic:**
- **Policies:** Child warriors, purge campaigns, defensive isolation, internal security
- **Edicts:** "Purge Outsiders", "Train Child Soldiers", "Fortify Borders", "Eliminate Threats"

**Fanatic Spiritual + Peaceful:**
- **Policies:** Centralized worship, contemplative practices, non-violence, harmony rituals
- **Edicts:** "Build Temple", "Observe Holy Day", "Meditate", "Promote Peace"

**Fanatic Materialist + Competitive:**
- **Policies:** Child workers, free market, industrial efficiency, wealth accumulation
- **Edicts:** "Expand Production", "Establish Markets", "Optimize Resources", "Privatize"

**Corrupt Spiritual + Warlike:**
- **Policies:** Child sacrifices, dark rituals, ruthless tactics, religious exploitation
- **Edicts:** "Conduct Blood Ritual", "Sacrifice for Power", "Holy War", "Dark Ceremony"

**Materialist + Authoritarian + Warlike:**
- **Policies:** War economy, forced labor, military hierarchy, production quotas
- **Edicts:** "Mobilize Industry", "Forced Conscription", "War Production", "Military Service"

**Peaceful + Egalitarian + Spiritual:**
- **Policies:** Democratic peace, shared resources, healing practices, harmony rituals
- **Edicts:** "Community Assembly", "Share Resources", "Provide Aid", "Promote Harmony"

---

## Naming Conventions

Entity and organization names reflect their **alignment**, **outlook combinations**, **village tech level**, **culture level**, and **education level**. Names evolve as these factors change.

### Alignment-Based Naming

**Lawful Entities:**
- **High Culture/Education:** Roman/Latin names (Marcus, Aurelia, Lucius, Valeria)
- **Medium Culture/Education:** Classical names (Alexander, Helena, Constantine, Sophia)
- **Low Culture/Education:** Formal names (John, Mary, William, Elizabeth)
- **Organizations:** "Order of...", "Legion of...", "Chapter of...", "Guild of..."

**Chaotic Entities:**
- **High Culture/Education:** Barbaric but sophisticated (Kael, Ragnar, Thora, Freya)
- **Medium Culture/Education:** Tribal names (Gorak, Zara, Korg, Nala)
- **Low Culture/Education:** Savage names (Gruk, Urk, Gak, Nak)
- **Organizations:** "Blood...", "Wild...", "Savage...", "Tribe of..."

**Neutral Entities:**
- **High Culture/Education:** Balanced names (Erik, Mira, Kael, Lina)
- **Medium Culture/Education:** Common names (Tom, Sarah, Mike, Anna)
- **Low Culture/Education:** Simple names (Bob, Sue, Jim, Pat)
- **Organizations:** "Company of...", "Group of...", "Band of...", "Circle of..."

### Tech Level Influence

**Primitive Tech:**
- **Lawful:** Tribal formal names (Chief Marcus, Elder Valeria)
- **Chaotic:** Savage names (Warrior Gruk, Shaman Urk)
- **Neutral:** Simple names (Hunter Tom, Gatherer Sue)

**Medieval Tech:**
- **Lawful:** Classical names (Knight Marcus, Lady Aurelia)
- **Chaotic:** Barbaric names (Raider Kael, Warrior Thora)
- **Neutral:** Common names (Merchant Erik, Farmer Mira)

**Industrial Tech:**
- **Lawful:** Formal industrial names (Engineer Lucius, Manager Valeria)
- **Chaotic:** Wild industrial names (Mechanic Ragnar, Worker Zara)
- **Neutral:** Common industrial names (Worker Tom, Clerk Sarah)

**Advanced Tech:**
- **Lawful:** Refined names (Director Marcus, Scientist Aurelia)
- **Chaotic:** Sophisticated wild names (Technician Kael, Engineer Freya)
- **Neutral:** Balanced advanced names (Specialist Erik, Analyst Mira)

### Culture Level Influence

**Low Culture:**
- Simple, direct names
- Basic organizational names
- Few honorifics or titles

**Medium Culture:**
- Standard names with titles
- Formal organizational names
- Common honorifics (Sir, Lady, Chief)

**High Culture:**
- Elaborate names with multiple titles
- Ornate organizational names
- Complex honorifics (Lord, Lady, Master, Grand)

### Education Level Influence

**Uneducated:**
- Simple names, basic vocabulary
- Direct organizational names
- Few abstract concepts

**Educated:**
- Sophisticated names, complex vocabulary
- Abstract organizational names
- Philosophical concepts

**Highly Educated:**
- Elaborate names, academic vocabulary
- Intellectual organizational names
- Complex philosophical concepts

### Outlook Combination Naming Patterns

**Fanatic Warlike + Xenophobic:**
- **Lawful:** "Legion of Purity", "Order of Purification", "Defense Chapter"
- **Chaotic:** "Blood Hounds", "Savage Purifiers", "Wild Purge"
- **Neutral:** "Purge Companies", "Defense Groups", "Purification Bands"

**Fanatic Spiritual + Peaceful:**
- **Lawful:** "Order of Serenity", "Temple of Contemplation", "Divine Circle"
- **Chaotic:** "Wild Mystics", "Savage Contemplatives", "Natural Harmony"
- **Pure:** "Divine Contemplatives", "Sacred Peace", "Holy Serenity"

**Warlike + Materialist + Authoritarian:**
- **Lawful:** "Merchant Legion", "Trade Order", "Commercial Chapter"
- **Chaotic:** "Wild Traders", "Savage Merchants", "Blood Commerce"
- **Corrupt:** "Oligarch Companies", "Exploitative Guilds", "Greed Houses"

### Village-Level Naming

**Village names** reflect the dominant alignment + outlook combination of the population:

**Lawful Good Pure Warlike Xenophobic:**
- "Fortress of Purity", "Defense Hold", "Purification Keep"

**Chaotic Evil Corrupt Peaceful Xenophilic:**
- "Wild Sanctuary", "Savage Haven", "Blood Peace"

**Neutral Materialist Competitive:**
- "Trade City", "Merchant Town", "Commerce Hub"

### Dynamic Naming Evolution

Names can evolve as entities/villages change:
- **Tech Advancement:** "Hunter Tom" → "Engineer Tom" → "Director Tom"
- **Culture Growth:** "Band of Warriors" → "Order of Warriors" → "Grand Order of Warriors"
- **Education:** "Farmer Bob" → "Scholar Bob" → "Master Scholar Bob"
- **Alignment Shift:** "Wild Raiders" → "Ordered Raiders" (if village becomes more lawful)

---

## Combination Examples

### Example 1: Lawful Good Pure Warlike Xenophobic Authoritarian Materialist (Might, Cooperative, Vengeful, Bold)

**Archetype:** "Roman Legion - Defensive Isolationists"
- **Aesthetics:** Structured Roman architecture, defensive fortifications, military precision
- **Visual:** Clean lines, defensive structures, military hierarchy, bold presence
- **Themes:** Defensive warfare, structured honor, isolated but strong, principled combat
- **Colors:** Deep blues, golds, bold contrasts, military palettes
- **Expression:** Committed to defensive warfare, structured military, isolated but honorable

### Example 2: Chaotic Evil Corrupt Peaceful Xenophilic Egalitarian Spiritual (Magic, Competitive, Forgiving, Craven)

**Archetype:** "Manipulative Cult - False Peace"
- **Aesthetics:** Organic but twisted, open but manipulative, elegant but corrupt
- **Visual:** Flowing but tarnished, open but dangerous, elegant but hollow
- **Themes:** False peace, manipulative openness, corrupt spirituality, competitive exploitation
- **Colors:** Soft but dark, elegant but twisted, subtle but dangerous
- **Expression:** Appears peaceful and open, but corrupt and competitive, uses magic for manipulation

### Example 3: Neutral Good Pure Warlike Neutral Egalitarian Materialist (Balanced, Cooperative, Neutral, Bold)

**Archetype:** "Democratic Warriors - Principled Defense"
- **Aesthetics:** Functional military, egalitarian structures, principled combat
- **Visual:** Balanced designs, shared military, bold but fair
- **Themes:** Defensive warfare, equal participation, principled combat, shared defense
- **Colors:** Balanced palettes, bold but fair, military but egalitarian
- **Expression:** Committed to defensive warfare, egalitarian participation, principled and bold

### Example 4: Lawful Neutral Corrupt Warlike Xenophobic Authoritarian Materialist (Might, Competitive, Vengeful, Bold)

**Archetype:** "Brutal Mercenaries - Selfish Hierarchy"
- **Aesthetics:** Structured but brutal, hierarchical but selfish, military but exploitative
- **Visual:** Clean but harsh, structured but tarnished, bold but cruel
- **Themes:** Selfish warfare, hierarchical exploitation, competitive brutality, vengeful order
- **Colors:** Dark metals, harsh contrasts, bold but cruel palettes
- **Expression:** Structured warfare for personal gain, hierarchical exploitation, competitive and vengeful

---

## Neutral/Balanced Expressions

When axes are near **neutral (0)** or **balanced**, the aesthetic expression tends toward:

### Visual Characteristics
- **Colors:** Grays, muted tones, balanced palettes, neutral hues
- **Forms:** Basic, functional, neither extreme, adaptable
- **Materials:** Common, versatile, neither precious nor crude
- **Structures:** Practical, balanced, context-appropriate

### Thematic Characteristics
- **Approach:** Pragmatic, context-dependent, balanced
- **Values:** Moderate, flexible, adaptable
- **Expression:** Neither extreme, balanced presentation

### Examples
- **Neutral Alignment + Neutral Outlooks:** Basic, functional, gray aesthetics, practical themes
- **Balanced Might/Magic:** Integrated magitech, balanced power sources
- **Neutral Behaviors:** Moderate expressions, context-appropriate responses

---

## Cross-Game Applications

### Godgame
- **Villagers:** Visual appearance based on alignment + outlook + behavior combinations
- **Villages:** Architectural style, cultural expressions, visual identity
- **Miracles:** Visual effects match god's alignment + outlook
- **Buildings:** Material and style based on village culture

### Space4X
- **Crews:** Uniforms, ship aesthetics, cultural expressions
- **Factions:** Visual identity, architectural styles, ship designs
- **Colonies:** Settlement aesthetics, cultural buildings, visual themes
- **Ships:** Design language based on faction alignment + outlook

### Shared Systems
- **UI/UX:** Color schemes, visual styling based on entity alignment/outlook
- **Tooltips:** Visual presentation matches entity archetype
- **Presentation:** Visual effects, animations, styling based on combinations
- **Narrative:** Tone, language, cultural expressions match archetypes

---

## Implementation Notes

### Visual System Integration
- **Color Palettes:** Define color schemes per alignment/outlook/behavior combination
- **Material Sets:** Define material libraries per archetype
- **Architectural Styles:** Define building/structural styles per combination
- **Clothing/Uniforms:** Define apparel styles per archetype

### Data-Driven Approach
- **Archetype Catalog:** Blob asset defining all combinations and their visual expressions
- **Theme Assets:** ScriptableObjects for visual themes per archetype
- **Material Libraries:** Asset collections per archetype
- **Color Schemes:** Palettes per combination

### Performance Considerations
- **LOD System:** Simpler visuals for distant entities
- **Caching:** Cache archetype visuals per entity
- **Batch Rendering:** Group entities by archetype for efficient rendering

---

## Related Documentation

- **Entity Stats & Archetypes:** `Concepts/Core/Entity_Stats_And_Archetypes_Canonical.md`
- **Alignment Framework:** `Concepts/Meta/Generalized_Alignment_Framework.md`
- **Behavior Systems:** `Archive/BehaviorAlignment_Summary.md`
- **Presentation Architecture:** `PresentationBridgeArchitecture.md`

---

## Open Questions

1. **Visual Complexity:** How detailed should archetype visuals be? (Simple color shifts vs full architectural styles)
2. **Blending:** How to handle entities with mixed archetypes? (Weighted blending vs dominant archetype)
3. **Progression:** Do archetypes evolve visually as entities age/grow? (Visual progression system)
4. **Player Customization:** Can players customize archetype visuals? (Modding support)
5. **Performance Budget:** What's the visual complexity budget per entity? (LOD requirements)

---

**For Designers:** Use this mapping to create consistent visual identities across all entity types. Each combination should feel distinct and immediately recognizable.

**For Implementers:** Create data-driven archetype system that maps combinations to visual assets. Support runtime archetype changes with visual updates.

**For Artists:** Use these mappings as starting points for visual design. Each archetype should have unique visual language while maintaining game-wide consistency.

---

**Last Updated:** 2025-01-XX  
**Status:** Draft - Ready for visual design iteration and implementation planning

