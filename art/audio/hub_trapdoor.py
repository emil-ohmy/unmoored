"""The trapdoor in the floor of the Ferryman's hut, heaved open from below (the way up out of the Hold), swinging over
and falling back flat onto the boards. A dull knock as the lid lifts off its frame, grit sifting off it, the old iron
hinges creaking long and slow as it's pushed up (the stick-slip quickening as it goes, the planks of the lid groaning
low under the strain), a breath of air as it tips past upright and drops, then the slam: a heavy, hollow wooden thud
on the floorboards (the floor booming under it, the boards rattling, the hinge irons clinking), and dust settling.
Inside a small wooden hut: a short, dry room.
Variants: 1 one long slow heave, a clean slam (2.6 s with the tail); 2 a shorter, harder shove whose hinge catches
halfway (creak, stop, creak), the lid bouncing once on the boards (2.4 s).
Uses laden_slam_windup's creak (stick-slip through wooden resonances). `synth` and `np` are injected by
tools/audio/build_sound.py."""
VARIANTS = 2

SR = synth.SR
HINGE = [(240, 1.0, 0.016), (590, 0.7, 0.011), (1350, 0.35, 0.007), (2600, 0.12, 0.004)]  # iron on old wood
LID = [(150, 1.0, 0.02), (330, 0.5, 0.012)]                                              # the lid's planks, low


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def shape(x, gain):
    freqs = np.fft.rfftfreq(len(x), 1 / SR)
    return np.fft.irfft(np.fft.rfft(x) * gain(freqs), len(x))


def band(x, centre, width):
    return shape(x, lambda f: np.exp(-0.5 * (np.log(np.maximum(f, 1) / centre) / width) ** 2))


def onset(d, seconds):
    return np.sin(np.clip(synth.seconds(d) / seconds, 0, 1) * np.pi / 2) ** 2


def swell(d, attack, release):
    t = synth.seconds(d)
    return np.sin(np.minimum(np.clip(t / attack, 0, 1), np.clip((d - t) / release, 0, 1)) * np.pi / 2) ** 2


def kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("slam", os.path.join(os.path.dirname(__file__), "laden_slam_windup.py"))
    module = importlib.util.module_from_spec(spec)
    module.synth, module.np = synth, np
    spec.loader.exec_module(module)
    return module


def unseat(rng, gain=1.0):
    """The lid lifting off its frame: a dull wooden knock from below, a little rattle of it in the frame."""
    d = 0.35
    t = synth.seconds(d)
    knock = np.sin(2 * np.pi * 105 * t) * np.exp(-t / 0.045) + 0.6 * np.sin(2 * np.pi * 260 * t) * np.exp(-t / 0.025)
    knock += 0.5 * norm(band(rng.normal(0, 1, len(t)), 700, 0.5)) * np.exp(-t / 0.015)
    knock *= onset(d, 0.003)
    for _ in range(3):
        c = band(rng.normal(0, 1, int(0.03 * SR)), rng.uniform(900, 1600), 0.4) * np.exp(-synth.seconds(0.03) / 0.006) * onset(0.03, 0.001)
        synth.place(knock, norm(c) * rng.uniform(0.15, 0.3), rng.uniform(0.03, 0.12))
    return norm(knock) * gain


def heave(k, d, rng, rate_from, rate_to, gain=1.0):
    """The hinges creaking as the lid is pushed up, and the lid's planks groaning low under the strain."""
    hinge = k.creak(d, rng, rate_from, rate_to, HINGE, lambda u: 0.6 + 0.4 * np.sin(np.pi * u))
    hinge = norm(hinge) * swell(d, d * 0.25, d * 0.3)
    groan = k.creak(d, rng, rate_from * 0.45, rate_to * 0.4, LID, lambda u: 1.0)
    groan = norm(groan) * swell(d, d * 0.4, d * 0.4)
    return shape(hinge * 0.75 + groan * 0.4, lambda f: 1 / (1 + (f / 2400) ** 2)) * gain  # (old, dull wood: no sharp ticks)


def grit(d, rng, count=60):
    """Dust and grit sifting off the lid as it rises: fine soft ticks, thinning."""
    out = np.zeros(int(d * SR))
    for at in np.sort(rng.random(count) ** 1.5 * d * 0.9):
        i = int(at * SR)
        m = int(rng.uniform(0.0006, 0.0015) * SR)
        out[i:i + m] += np.hanning(m) * rng.uniform(-1, 1)
    return norm(band(out, 4200, 0.6)) * swell(d, 0.05, d * 0.6)


def fall_air(rng, d=0.35):
    """A breath of air as the lid swings over and drops."""
    return norm(band(rng.normal(0, 1, int(d * SR)), 500, 0.7)) * swell(d, d * 0.7, d * 0.15)


def slam(rng, gain=1.0, rattle=1.0):
    """The lid flat onto the floorboards: a heavy hollow thud (the floor booms under it), a sharp wooden smack on
    top, the boards rattling, the hinge irons clinking."""
    d = 0.9
    t = synth.seconds(d)
    f = 70 + 45 * np.exp(-t / 0.03)
    boom = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.11)
    boards = sum(a * np.sin(2 * np.pi * fr * t + rng.uniform(0, 6)) * np.exp(-t / tau)
                 for fr, a, tau in ((185, 0.6, 0.07), (310, 0.45, 0.05), (520, 0.3, 0.035)))
    smack = norm(band(rng.normal(0, 1, len(t)), 1100, 0.7)) * np.exp(-t / 0.012)
    out = (norm(boom) * 1.0 + norm(boards) * 0.6 + smack * 0.55) * onset(d, 0.002)
    shake = np.zeros_like(t)  # the boards rattling: a quick, thinning chatter
    at = 0.02
    while at < 0.3:
        c = band(rng.normal(0, 1, int(0.02 * SR)), rng.uniform(600, 1300), 0.35) * np.exp(-synth.seconds(0.02) / 0.004) * onset(0.02, 0.001)
        synth.place(shake, norm(c) * (1 - at / 0.3) * rng.uniform(0.5, 1.0), at)
        at += rng.uniform(0.018, 0.04)
    out += shake * 0.3 * rattle
    for _ in range(2):  # the hinge irons
        cd = 0.25
        ct = synth.seconds(cd)
        clink = sum(np.sin(2 * np.pi * fr * ct) * np.exp(-ct / tau) for fr, tau in ((2350, 0.03), (3720, 0.018), (5100, 0.012)))
        synth.place(out, clink * onset(cd, 0.0008) * rng.uniform(0.025, 0.045), rng.uniform(0.01, 0.06))
    return out * gain


def room(x, rng):
    """A small wooden hut: a few close reflections, a short dry tail."""
    ir = synth.reverb_ir(0.6, 0.45, rng, predelay=0.006, brightness=0.25)[0]
    wet = synth.fft_convolve(x, ir)[:len(x)]
    return x + norm(wet) * np.max(np.abs(x)) * 0.22


def render(v, rng):
    k = kit()
    if v == 0:
        total = 2.6
        out = np.zeros(int(total * SR))
        synth.place(out, unseat(rng, 0.6), 0.0)
        synth.place(out, heave(k, 1.1, rng, 18, 42, 0.75), 0.08)
        synth.place(out, grit(1.0, rng) * 0.12, 0.1)
        synth.place(out, fall_air(rng) * 0.12, 1.05)
        synth.place(out, slam(rng), 1.38)
        synth.place(out, grit(0.8, rng, 40) * 0.08, 1.45)  # dust settling
    else:
        total = 2.4
        out = np.zeros(int(total * SR))
        synth.place(out, unseat(rng, 0.75), 0.0)
        synth.place(out, heave(k, 0.42, rng, 20, 36, 0.75), 0.06)
        synth.place(out, heave(k, 0.5, rng, 26, 48, 0.85), 0.62)  # (it caught: a harder shove)
        synth.place(out, grit(0.9, rng) * 0.12, 0.08)
        synth.place(out, fall_air(rng, 0.28) * 0.12, 1.02)
        synth.place(out, slam(rng, 1.0, 1.3), 1.26)
        synth.place(out, slam(rng, 0.3, 0.6) * synth.decay(0.9, 0.3), 1.39)  # a small bounce
        synth.place(out, grit(0.7, rng, 40) * 0.08, 1.32)
    t = synth.seconds(total)
    return room(out, rng) * np.cos(np.clip((t - (total - 0.25)) / 0.25, 0, 1) * np.pi / 2) ** 2
