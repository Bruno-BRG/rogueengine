use serde::{Deserialize, Serialize};

use crate::AssetError;

pub type Color = [u8; 4];
pub const TRANSPARENT: Color = [0, 0, 0, 0];

/// A multi-frame RGBA sprite. Frames share one size; animation timing lives in `animation`.
#[derive(Clone, Debug, PartialEq, Serialize, Deserialize)]
pub struct Sprite {
    pub name: String,
    pub width: u32,
    pub height: u32,
    /// Each frame is `width*height*4` RGBA bytes.
    pub frames: Vec<Vec<u8>>,
    #[serde(default)]
    pub palette: Vec<Color>,
}

impl Sprite {
    pub fn new(name: &str, width: u32, height: u32) -> Self {
        Self { name: name.into(), width, height, frames: vec![vec![0; (width * height * 4) as usize]], palette: vec![] }
    }

    fn idx(&self, x: i32, y: i32) -> Option<usize> {
        (x >= 0 && y >= 0 && (x as u32) < self.width && (y as u32) < self.height).then(|| ((y as u32 * self.width + x as u32) * 4) as usize)
    }

    pub fn get(&self, frame: usize, x: i32, y: i32) -> Option<Color> {
        let i = self.idx(x, y)?;
        let f = self.frames.get(frame)?;
        Some([f[i], f[i + 1], f[i + 2], f[i + 3]])
    }

    pub fn set(&mut self, frame: usize, x: i32, y: i32, c: Color) {
        if let (Some(i), Some(f)) = (self.idx(x, y), self.frames.get_mut(frame)) {
            f[i..i + 4].copy_from_slice(&c);
        }
    }

    /// Encode one frame as PNG.
    pub fn frame_png(&self, frame: usize) -> Result<Vec<u8>, AssetError> {
        let data = self.frames.get(frame).ok_or_else(|| AssetError::Invalid(format!("no frame {frame}")))?;
        encode_png(self.width, self.height, data)
    }

    /// All frames in one horizontal strip PNG.
    pub fn sheet_png(&self) -> Result<Vec<u8>, AssetError> {
        let n = self.frames.len() as u32;
        let mut out = vec![0u8; (self.width * n * self.height * 4) as usize];
        for (fi, f) in self.frames.iter().enumerate() {
            for y in 0..self.height as usize {
                let src = y * self.width as usize * 4;
                let dst = (y * (self.width * n) as usize + fi * self.width as usize) * 4;
                out[dst..dst + self.width as usize * 4].copy_from_slice(&f[src..src + self.width as usize * 4]);
            }
        }
        encode_png(self.width * n, self.height, &out)
    }

    /// Import a PNG as a single-frame sprite, or slice it into `frame_w`-wide frames.
    pub fn from_png(name: &str, bytes: &[u8], frame_w: Option<u32>) -> Result<Self, AssetError> {
        let mut dec = png::Decoder::new(bytes);
        dec.set_transformations(png::Transformations::EXPAND | png::Transformations::STRIP_16);
        let mut reader = dec.read_info().map_err(|e| AssetError::Png(e.to_string()))?;
        let mut buf = vec![0; reader.output_buffer_size()];
        let info = reader.next_frame(&mut buf).map_err(|e| AssetError::Png(e.to_string()))?;
        let rgba: Vec<u8> = match info.color_type {
            png::ColorType::Rgba => buf[..info.buffer_size()].to_vec(),
            png::ColorType::Rgb => buf[..info.buffer_size()].chunks(3).flat_map(|p| [p[0], p[1], p[2], 255]).collect(),
            png::ColorType::GrayscaleAlpha => buf[..info.buffer_size()].chunks(2).flat_map(|p| [p[0], p[0], p[0], p[1]]).collect(),
            png::ColorType::Grayscale => buf[..info.buffer_size()].iter().flat_map(|&g| [g, g, g, 255]).collect(),
            png::ColorType::Indexed => return Err(AssetError::Png("indexed png not expanded".into())),
        };
        let fw = frame_w.unwrap_or(info.width).clamp(1, info.width);
        if info.width % fw != 0 {
            return Err(AssetError::Invalid(format!("width {} is not a multiple of frame width {fw}", info.width)));
        }
        let n = info.width / fw;
        let mut s = Sprite { name: name.into(), width: fw, height: info.height, frames: vec![], palette: vec![] };
        for fi in 0..n {
            let mut f = Vec::with_capacity((fw * info.height * 4) as usize);
            for y in 0..info.height {
                let start = ((y * info.width + fi * fw) * 4) as usize;
                f.extend_from_slice(&rgba[start..start + (fw * 4) as usize]);
            }
            s.frames.push(f);
        }
        Ok(s)
    }
}

fn encode_png(w: u32, h: u32, rgba: &[u8]) -> Result<Vec<u8>, AssetError> {
    let mut out = Vec::new();
    {
        let mut enc = png::Encoder::new(&mut out, w, h);
        enc.set_color(png::ColorType::Rgba);
        enc.set_depth(png::BitDepth::Eight);
        let mut wr = enc.write_header().map_err(|e| AssetError::Png(e.to_string()))?;
        wr.write_image_data(rgba).map_err(|e| AssetError::Png(e.to_string()))?;
    }
    Ok(out)
}

/// Editing session over a sprite: drawing tools plus undo/redo.
/// Every public mutating call is one undo step, so the UI can stay dumb.
pub struct SpriteEditor {
    pub sprite: Sprite,
    pub frame: usize,
    undo: Vec<Sprite>,
    redo: Vec<Sprite>,
}

const MAX_UNDO: usize = 100;

impl SpriteEditor {
    pub fn new(sprite: Sprite) -> Self {
        Self { sprite, frame: 0, undo: vec![], redo: vec![] }
    }

    fn checkpoint(&mut self) {
        self.undo.push(self.sprite.clone());
        if self.undo.len() > MAX_UNDO {
            self.undo.remove(0);
        }
        self.redo.clear();
    }
    pub fn undo(&mut self) -> bool {
        match self.undo.pop() {
            Some(prev) => {
                self.redo.push(std::mem::replace(&mut self.sprite, prev));
                self.frame = self.frame.min(self.sprite.frames.len() - 1);
                true
            }
            None => false,
        }
    }
    pub fn redo(&mut self) -> bool {
        match self.redo.pop() {
            Some(next) => {
                self.undo.push(std::mem::replace(&mut self.sprite, next));
                self.frame = self.frame.min(self.sprite.frames.len() - 1);
                true
            }
            None => false,
        }
    }

    pub fn pixel(&mut self, x: i32, y: i32, c: Color) {
        self.checkpoint();
        self.sprite.set(self.frame, x, y, c);
    }

    /// Freehand stroke: one undo step for the whole line (Bresenham, inclusive of both ends).
    pub fn line(&mut self, a: (i32, i32), b: (i32, i32), c: Color) {
        self.checkpoint();
        for (x, y) in bresenham(a, b) {
            self.sprite.set(self.frame, x, y, c);
        }
    }

    pub fn rect(&mut self, a: (i32, i32), b: (i32, i32), c: Color, filled: bool) {
        self.checkpoint();
        let (x0, x1, y0, y1) = (a.0.min(b.0), a.0.max(b.0), a.1.min(b.1), a.1.max(b.1));
        for y in y0..=y1 {
            for x in x0..=x1 {
                if filled || x == x0 || x == x1 || y == y0 || y == y1 {
                    self.sprite.set(self.frame, x, y, c);
                }
            }
        }
    }

    /// 4-connected flood fill of the region containing (x, y).
    pub fn fill(&mut self, x: i32, y: i32, c: Color) {
        let Some(target) = self.sprite.get(self.frame, x, y) else { return };
        if target == c {
            return;
        }
        self.checkpoint();
        let mut stack = vec![(x, y)];
        while let Some((px, py)) = stack.pop() {
            if self.sprite.get(self.frame, px, py) != Some(target) {
                continue;
            }
            self.sprite.set(self.frame, px, py, c);
            stack.extend([(px + 1, py), (px - 1, py), (px, py + 1), (px, py - 1)]);
        }
    }

    pub fn flip_horizontal(&mut self) {
        self.checkpoint();
        let (w, h) = (self.sprite.width as i32, self.sprite.height as i32);
        for y in 0..h {
            for x in 0..w / 2 {
                let (a, b) = (self.sprite.get(self.frame, x, y).unwrap(), self.sprite.get(self.frame, w - 1 - x, y).unwrap());
                self.sprite.set(self.frame, x, y, b);
                self.sprite.set(self.frame, w - 1 - x, y, a);
            }
        }
    }

    pub fn add_frame(&mut self, duplicate_current: bool) -> usize {
        self.checkpoint();
        let f = if duplicate_current { self.sprite.frames[self.frame].clone() } else { vec![0; self.sprite.frames[0].len()] };
        self.sprite.frames.insert(self.frame + 1, f);
        self.frame += 1;
        self.frame
    }

    pub fn remove_frame(&mut self) -> bool {
        if self.sprite.frames.len() <= 1 {
            return false;
        }
        self.checkpoint();
        self.sprite.frames.remove(self.frame);
        self.frame = self.frame.min(self.sprite.frames.len() - 1);
        true
    }
}

fn bresenham(a: (i32, i32), b: (i32, i32)) -> Vec<(i32, i32)> {
    let (mut x, mut y) = a;
    let (dx, dy) = ((b.0 - a.0).abs(), -(b.1 - a.1).abs());
    let (sx, sy) = (if a.0 < b.0 { 1 } else { -1 }, if a.1 < b.1 { 1 } else { -1 });
    let mut err = dx + dy;
    let mut out = vec![(x, y)];
    while (x, y) != b {
        let e2 = 2 * err;
        if e2 >= dy { err += dy; x += sx; }
        if e2 <= dx { err += dx; y += sy; }
        out.push((x, y));
    }
    out
}

/// Serializable editor command: the whole sprite-editor protocol (UI -> Rust).
#[derive(Clone, Debug, Deserialize)]
#[serde(tag = "op", rename_all = "snake_case")]
pub enum SpriteOp {
    Pixel { x: i32, y: i32, color: Color },
    Line { x0: i32, y0: i32, x1: i32, y1: i32, color: Color },
    Rect { x0: i32, y0: i32, x1: i32, y1: i32, color: Color, #[serde(default)] filled: bool },
    Fill { x: i32, y: i32, color: Color },
    FlipHorizontal,
    AddFrame { #[serde(default)] duplicate: bool },
    RemoveFrame,
    SelectFrame { index: usize },
    Undo,
    Redo,
}

/// What the UI needs to redraw: the current frame's RGBA pixels plus frame navigation info.
#[derive(Clone, Debug, Serialize)]
pub struct SpriteView {
    pub name: String,
    pub width: u32,
    pub height: u32,
    pub frame: usize,
    pub frame_count: usize,
    pub pixels: Vec<u8>,
}

impl SpriteEditor {
    pub fn apply(&mut self, op: SpriteOp) {
        match op {
            SpriteOp::Pixel { x, y, color } => self.pixel(x, y, color),
            SpriteOp::Line { x0, y0, x1, y1, color } => self.line((x0, y0), (x1, y1), color),
            SpriteOp::Rect { x0, y0, x1, y1, color, filled } => self.rect((x0, y0), (x1, y1), color, filled),
            SpriteOp::Fill { x, y, color } => self.fill(x, y, color),
            SpriteOp::FlipHorizontal => self.flip_horizontal(),
            SpriteOp::AddFrame { duplicate } => {
                self.add_frame(duplicate);
            }
            SpriteOp::RemoveFrame => {
                self.remove_frame();
            }
            SpriteOp::SelectFrame { index } => self.frame = index.min(self.sprite.frames.len() - 1),
            SpriteOp::Undo => {
                self.undo();
            }
            SpriteOp::Redo => {
                self.redo();
            }
        }
    }

    pub fn view(&self) -> SpriteView {
        SpriteView {
            name: self.sprite.name.clone(),
            width: self.sprite.width,
            height: self.sprite.height,
            frame: self.frame,
            frame_count: self.sprite.frames.len(),
            pixels: self.sprite.frames[self.frame].clone(),
        }
    }
}
