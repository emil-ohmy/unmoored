# How Death's Door and Tunic handle death, currency, healing and checkpoints
Type: research
Status: resolved
Blocked by:

## Question

In Death's Door and Tunic specifically: what happens on death, what is lost and how it's retrieved,
what currency is for (given neither leans on stat grinding like Dark Souls), how healing works and refills,
what checkpoints do (and whether enemies respawn), and how equipment/upgrades are found.
Summarise per game, then note which patterns fit a game whose progression is abilities + equipment, with no stat upgrades.

## Answer

Full findings: `research/reference-death-and-economy.md` on branch `research/reference-death-and-economy`
(commit 789eb34). Read with `git show research/reference-death-and-economy:research/reference-death-and-economy.md`.

Premise correction: both games DO sell stat upgrades for currency, but capped (Death's Door: 5 levels × 4 stats;
Tunic: levels bought with finite, chest-found offering items). Neither grinds like Dark Souls.

- **Death's Door**: death costs nothing but the walk back from the last door. Shortcuts stay open. Enemies respawn
  on door use or death. Healing: found Life Seeds planted in fixed pots grow one-time full-heal fruit. Weapons
  are found in the world and differ in profile. Spells are won from bosses and double as traversal keys.
- **Tunic**: you drop coins and leave a ghost (touching it gives the coins back and stuns nearby enemies).
  How much you drop is unconfirmed. Potion flasks refill at checkpoints, and more are gained from shards or
  shops. Checkpoints fully heal and reset all enemies except bosses. Equipment comes from chests, plus passive
  cards in limited slots.
- **Both respawn enemies on rest/death.** Not picking "checkpoints with enemy respawn" in charting departs
  from both references.
- **Options that fit abilities + equipment with no stat upgrades:**
  (1) death costs time only;
  (2) drop-and-retrieve ghost with a pickup bonus;
  (3) currency buys equipment, slots and consumables, never stats;
  (4) Healing charges refill at checkpoints, and extra charges are found;
  (5) found passive modifiers and weapon profiles as the equipment layer.
  Suggested: 1 or 2, plus 3, 4 and 5. Still open: whether found max-HP or extra charges count as a "stat".
