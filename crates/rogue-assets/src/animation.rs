use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ClipFrame {
    /// Index into the sprite's frames.
    pub frame: usize,
    pub ms: u32,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Clip {
    pub name: String,
    pub frames: Vec<ClipFrame>,
    #[serde(default = "yes")]
    pub looping: bool,
}
fn yes() -> bool {
    true
}

impl Clip {
    pub fn duration_ms(&self) -> u32 {
        self.frames.iter().map(|f| f.ms.max(1)).sum()
    }
}

/// Plays clips. Time-driven by `update(dt_ms)`, so it is deterministic and testable;
/// the renderer just asks `frame()` each draw.
#[derive(Clone, Debug, Default)]
pub struct Animator {
    clips: HashMap<String, Clip>,
    current: Option<String>,
    elapsed: u32,
    finished: bool,
}

impl Animator {
    pub fn new(clips: Vec<Clip>) -> Self {
        Self { clips: clips.into_iter().map(|c| (c.name.clone(), c)).collect(), ..Default::default() }
    }

    /// Start a clip. Re-playing the clip already running is a no-op unless `restart`.
    pub fn play(&mut self, name: &str, restart: bool) -> bool {
        if !self.clips.contains_key(name) {
            return false;
        }
        if restart || self.current.as_deref() != Some(name) {
            self.current = Some(name.into());
            self.elapsed = 0;
            self.finished = false;
        }
        true
    }

    pub fn update(&mut self, dt_ms: u32) {
        let Some(clip) = self.current.as_ref().and_then(|n| self.clips.get(n)) else { return };
        let total = clip.duration_ms();
        if total == 0 || clip.frames.is_empty() {
            return;
        }
        self.elapsed += dt_ms;
        if self.elapsed >= total {
            if clip.looping {
                self.elapsed %= total;
            } else {
                self.elapsed = total;
                self.finished = true;
            }
        }
    }

    pub fn finished(&self) -> bool {
        self.finished
    }
    pub fn current(&self) -> Option<&str> {
        self.current.as_deref()
    }

    /// Sprite frame index to draw right now.
    pub fn frame(&self) -> Option<usize> {
        let clip = self.clips.get(self.current.as_ref()?)?;
        let mut t = self.elapsed;
        for f in &clip.frames {
            let d = f.ms.max(1);
            if t < d {
                return Some(f.frame);
            }
            t -= d;
        }
        clip.frames.last().map(|f| f.frame)
    }
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Transition {
    pub from: String,
    pub to: String,
    /// Fires on this named trigger (e.g. "attack", "hurt"), or "finished" when the clip ends.
    pub on: String,
}

/// Animation state machine: triggers (`attack`, `hurt`...) move between clips.
/// Games drive it from engine events, e.g. a `moved` hook calls `controller.trigger("walk")`.
#[derive(Clone, Debug, Default)]
pub struct Controller {
    pub animator: Animator,
    transitions: Vec<Transition>,
    pub default_clip: String,
}

impl Controller {
    pub fn new(clips: Vec<Clip>, transitions: Vec<Transition>, default_clip: &str) -> Self {
        let mut animator = Animator::new(clips);
        animator.play(default_clip, true);
        Self { animator, transitions, default_clip: default_clip.into() }
    }

    pub fn trigger(&mut self, name: &str) -> bool {
        let Some(cur) = self.animator.current().map(str::to_string) else { return false };
        match self.transitions.iter().find(|t| t.from == cur && t.on == name).map(|t| t.to.clone()) {
            Some(to) => self.animator.play(&to, true),
            None => false,
        }
    }

    pub fn update(&mut self, dt_ms: u32) {
        self.animator.update(dt_ms);
        if self.animator.finished() {
            self.trigger("finished");
            // a finished one-shot with no transition falls back to the default clip
            if self.animator.finished() {
                self.animator.play(&self.default_clip.clone(), true);
            }
        }
    }
}
