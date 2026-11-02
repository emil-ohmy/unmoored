"""The Hold's face letting its breath go at you (the gust that pushes you back), 1.5 s: an enormous exhalation
through stone. The Hold's own breath (hold_breath_in/out) made huge and close and blown out hard: a broad roar of
air rising fast (about a fifth of the way in) and blowing on, sinking slowly; grit and sand carried in it, ticking
off the stone, thick at the height; the cracks whistling a moment; the rock's deep tone under it; a breath-voice of
the souls in it (a low, rough "haaa", nearly all air). Variants: 1 plain, 2 darker and grittier.
Uses the_hold_wake's kit.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 2
PEAK = 0.75

SR = synth.SR


def kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("the_hold_wake", os.path.join(os.path.dirname(__file__), "the_hold_wake.py"))
    k = importlib.util.module_from_spec(spec)
    k.synth, k.np = synth, np
    spec.loader.exec_module(k)
    return k


def render(v, rng):
    k = kit()
    breath_kit = k.load("hold_breath_in")
    d = 1.5
    n = int(d * SR)
    t = synth.seconds(d)
    flow = k.curve(d, [(0, 0.0), (0.08, 0.35), (0.28, 1.0), (0.55, 0.85), (1.1, 0.35), (d, 0.0)], 0.05)
    size = [1.0, 1.3][v]
    out = k.norm(k.gust_air(d, flow, rng, inward=False, size=size))
    out += k.norm(k.sand(d, flow, rng, [2500, 4200][v])) * [0.22, 0.32][v] * flow
    out += k.norm(breath_kit.cracks(d, flow, False, rng, 24)) * 0.06
    out += k.norm(breath_kit.mass(d, flow, False, rng)) * 0.45
    voice = k.souls(d, rng, k.curve(d, [(0, 62), (d, 50)]), 5, "ah", rasp=0.6, breath=1.4, dark=1400)
    out += k.norm(voice) * flow ** 1.3 * 0.3
    out += k.norm(k.rubble(d, rng, 18, 0.15, 0.9, 0.3)) * 0.1
    out = synth.highpass(out, 22)
    out *= np.clip(t / 0.06, 0, 1)
    out = k.chamber(out, rng, 1.6, 0.35)
    return k.trim(out, -48, 0.35)
