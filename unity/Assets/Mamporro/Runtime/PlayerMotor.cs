using UnityEngine;

namespace Mamporro.U1
{
    public struct MoveIntent
    {
        public Vector2 direction;
        public bool jumpPressed, jumpHeld, slidePressed, slideHeld;
    }

    // Estado independiente de escena e Input System; el adaptador entrega intenciones.
    public sealed class PlayerMotor
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public bool Grounded { get; private set; }
        public bool Sliding { get; private set; }
        float coyote, buffer, cooldown;
        readonly PrototypeSettings settings;
        public PlayerMotor(PrototypeSettings settings) { this.settings = settings; Reset(); }
        public void Reset() { Position = new Vector3(0,0,-16); Velocity = Vector3.zero; Grounded = true; Sliding = false; coyote = buffer = cooldown = 0; }

        public void Step(MoveIntent intent, float dt)
        {
            var s = settings;
            var horizontal = new Vector3(Velocity.x,0,Velocity.z);
            var desired = new Vector3(intent.direction.x,0,intent.direction.y);
            desired = Vector3.ClampMagnitude(desired,1);
            var normal = TechnicalWorld.Normal(Position.x,Position.z);
            bool steep = normal.y < Mathf.Cos(s.slopeLimit*Mathf.Deg2Rad);
            coyote = Grounded && !steep ? s.coyoteSeconds : Mathf.Max(0,coyote-dt);
            buffer = intent.jumpPressed ? s.jumpBufferSeconds : Mathf.Max(0,buffer-dt);
            cooldown = Mathf.Max(0,cooldown-dt);
            if (intent.slidePressed && Grounded && cooldown <= 0)
            {
                var direction = horizontal.sqrMagnitude > .1f ? horizontal.normalized : desired;
                horizontal = direction * Mathf.Min(s.moveSpeed*1.8f,Mathf.Max(s.moveSpeed*1.45f,horizontal.magnitude+3.5f));
                Sliding = true; cooldown = .6f;
            }
            Sliding &= intent.slideHeld && Grounded && horizontal.magnitude > 3.2f;
            if (Sliding)
            {
                horizontal += Vector3.ProjectOnPlane(Vector3.down*s.gravity*1.25f,normal)*dt;
                horizontal.y = 0;
                horizontal = Vector3.MoveTowards(horizontal,Vector3.zero,4*dt);
                if (desired.sqrMagnitude > .01f)
                    horizontal = Vector3.RotateTowards(horizontal,desired*horizontal.magnitude,2.4f*dt,0);
                horizontal = Vector3.ClampMagnitude(horizontal,30);
            }
            else
            {
                float accel = Grounded ? (desired.sqrMagnitude > 0 ? s.groundAcceleration : s.deceleration) : s.airAcceleration;
                if (horizontal.magnitude > s.moveSpeed) accel = 9;
                horizontal = Vector3.MoveTowards(horizontal,desired*s.moveSpeed,accel*dt);
            }
            float vy = Velocity.y;
            if (buffer > 0 && coyote > 0)
            {
                vy = s.jumpSpeed; Grounded = false; buffer = coyote = 0;
                if (Sliding) horizontal = horizontal.normalized*Mathf.Min(horizontal.magnitude+1.5f,s.moveSpeed*1.8f);
                Sliding = false;
            }
            if (!Grounded) vy = Mathf.Max(-42,vy-s.gravity*(vy < 0 ? 1.55f : intent.jumpHeld ? 1 : 2.3f)*dt);
            if (steep && Grounded) horizontal += new Vector3(normal.x,0,normal.z)*s.gravity*dt;
            var delta = horizontal*dt;
            // Impide subir pendientes excesivas, manteniendo movimiento lateral y descenso.
            if (TechnicalWorld.Height(Position.x+delta.x,Position.z+delta.z) > Position.y && steep)
                delta = Vector3.ProjectOnPlane(delta,new Vector3(normal.x,0,normal.z).normalized);
            var next = TechnicalWorld.Move(Position,delta,s.radius,Grounded ? s.stepHeight : .02f);
            next.y += vy*dt;
            float ground = TechnicalWorld.Height(next.x,next.z);
            if (vy <= 0 && next.y <= ground + (Grounded ? .25f : 0)) { next.y = ground; vy = 0; Grounded = true; }
            else Grounded = false;
            Position = next;
            Velocity = new Vector3(horizontal.x,vy,horizontal.z);
        }
    }
}
