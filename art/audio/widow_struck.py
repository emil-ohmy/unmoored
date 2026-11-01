"""The Widow Hale struck inside running time (she reels, 0.6 s): a sharp crack like a clock's glass cracking (a hard snap,
the fracture running in a spray of tiny clicks, a thin bright ring of the glass), and behind the crape a gasp (a short
caught breath with a voice in it)."""


def render(v, rng):
    duration = 0.6
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    # The snap.
    d = 0.05
    tt = synth.seconds(d)
    snap = synth.highpass(synth.white(d, rng), 2500) * np.exp(-tt / 0.004) * np.clip(tt / 0.0004, 0, 1)
    synth.place(out, snap / (np.max(np.abs(snap)) + 1e-9) * 0.75, 0.0)
    # The fracture running: tiny clicks spraying out, thinning.
    at = 0.004
    while at < 0.09:
        d = 0.01
        tt = synth.seconds(d)
        c = synth.bandpass(synth.white(d, rng), rng.uniform(3500, 8000), 3.0) * np.exp(-tt / 0.0015)
        synth.place(out, c / (np.max(np.abs(c)) + 1e-9) * rng.uniform(0.15, 0.4) * (1 - at / 0.1), at)
        at += rng.uniform(0.003, 0.012)
    # The glass ringing, thin and short (a watch crystal, inharmonic).
    d = 0.4
    tt = synth.seconds(d)
    ring = sum(a * np.sin(2 * np.pi * f * tt + rng.uniform(0, 6)) * np.exp(-tt / tau)
               for f, a, tau in ((3870, 1.0, 0.08), (5310, 0.6, 0.05), (7420, 0.35, 0.03), (2610, 0.3, 0.1)))
    ring *= np.clip(tt / 0.001, 0, 1)
    synth.place(out, ring / (np.max(np.abs(ring)) + 1e-9) * 0.18, 0.002)
    # A low knock under it: the blow landing.
    d = 0.15
    tt = synth.seconds(d)
    body = synth.lowpass(synth.white(d, rng), 400) * np.exp(-tt / 0.025) * np.clip(tt / 0.002, 0, 1)
    synth.place(out, body / (np.max(np.abs(body)) + 1e-9) * 0.35, 0.0)
    # The gasp behind the crape: a quick caught inhale, a voice catching in it.
    start = 0.07
    d = 0.45
    tt = synth.seconds(d)
    gasp = synth.bandpass(synth.white(d, rng), synth.glide(700, 1700, d, 0.7), 1.2)
    genv = np.clip(tt / 0.05, 0, 1) * np.exp(-tt / 0.16) * np.clip((d - tt) / 0.1, 0, 1)
    synth.place(out, gasp / (np.max(np.abs(gasp)) + 1e-9) * genv * 0.42, start)
    vd = 0.16
    cry = synth.voice(synth.glide(synth.note("B3"), synth.note("E4"), vd, 0.5), vd, rng, vowel="ah", vibrato=0.01)
    cry = synth.lowpass(cry, 2000) * synth.envelope(vd, 0.03, 0, curve=2.0, peak_at=0.05)
    synth.place(out, cry / (np.max(np.abs(cry)) + 1e-9) * 0.2, start + 0.01)
    out = synth.highpass(out, 30)  # (no DC left under the silence)
    fade = int(0.05 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
