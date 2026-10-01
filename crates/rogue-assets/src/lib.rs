//! Assets: sprites (pixel editing core for the built-in editor) and animation.
pub mod animation;
pub mod sprite;

pub use animation::{Animator, Clip, ClipFrame, Controller, Transition};
pub use sprite::{Color, Sprite, SpriteEditor, SpriteOp, SpriteView};

#[derive(Debug, thiserror::Error)]
pub enum AssetError {
    #[error("png: {0}")]
    Png(String),
    #[error("{0}")]
    Invalid(String),
}
