using UnityEngine;

namespace Mamporro.U1
{
    // Punto de extensión para reutilizar cámara, movimiento y presentación de U1.
    // Una escena tiene una sola simulación; U1 conserva su horda técnica.
    public abstract class PrototypeSession : MonoBehaviour
    {
        public abstract bool AllowResume { get; }
        public virtual bool Measuring => false;
        public abstract void Initialize(PrototypeController controller);
        public abstract void UpdateSession();
        public abstract void BeforeMovement();
        public abstract void AfterMovement();
        public abstract void Draw();
        public abstract void RefreshHud();
    }
}
