"""A shortcut gate opened: the heavy barring plank scraped off its brackets and dropped, then a long, groaning hinge
creak as the door swings, ending in a dull thud against its stop. The sound of the way back opening."""


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("slam", os.path.join(os.path.dirname(__file__), "laden_slam_windup.py"))
    slam = importlib.util.module_from_spec(spec)
    slam.synth, slam.np = synth, np
    spec.loader.exec_module(slam)
    duration = 2.2
    out = np.zeros(int(duration * synth.SR))
    scrape = synth.bandpass(synth.white(0.35, rng), synth.glide(900, 600, 0.35), 2.0) * synth.envelope(0.35, 0.05, 0, curve=1.5)
    drop = synth.bandpass(synth.white(0.12, rng), 420, 2.5) * synth.decay(0.12, 0.03)
    drop_body = synth.sine(synth.glide(160, 90, 0.15), 0.15) * synth.decay(0.15, 0.04)
    synth.place(out, slam.norm(scrape) * 0.4, 0.0)
    synth.place(out, slam.norm(drop) * 0.6, 0.38)
    synth.place(out, drop_body * 0.5, 0.38)
    hinge_len = 1.1
    creak = slam.creak(hinge_len, rng, 18, 45, [(260, 1.0, 0.014), (610, 0.7, 0.01), (1300, 0.35, 0.006)], lambda u: 1.0)
    creak *= synth.envelope(hinge_len, 0.1, 0, curve=0.8, peak_at=0.5)
    synth.place(out, slam.norm(creak) * 0.7, 0.55)
    thud = synth.sine(synth.glide(120, 60, 0.3), 0.3) * synth.decay(0.3, 0.07)
    knock = synth.bandpass(synth.white(0.08, rng), 500, 2.0) * synth.decay(0.08, 0.02)
    synth.place(out, thud * 0.7, 1.62)
    synth.place(out, slam.norm(knock) * 0.5, 1.62)
    room = synth.reverb_ir(1.2, 1.0, rng, brightness=0.25)[0]
    wet = synth.fft_convolve(out, room)[:len(out)]
    return out + wet / (np.max(np.abs(wet)) + 1e-9) * 0.2
