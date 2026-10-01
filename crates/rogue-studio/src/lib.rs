//! Studio: everything the editor does that isn't running the game —
//! projects on disk, sprites, graphs, scripts, objects — behind one JSON command API.
pub mod api;
pub mod art;
pub mod project;
pub mod server;
mod template;

pub use api::Studio;
pub use project::Project;
