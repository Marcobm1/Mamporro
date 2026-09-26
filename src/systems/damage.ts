// Fórmulas de daño (lógica pura, cubierta por tests).

export interface DamageRoll {
  amount: number;
  /** 0 = normal, 1 = crítico, 2 o más = supercrítico. */
  critLevel: number;
}

/**
 * Tira el daño de un golpe. La probabilidad de crítico puede pasar de 1 (100 %):
 * cada 100 % completo es un nivel de crítico seguro y el resto, una probabilidad
 * de un nivel más (150 % = crítico seguro y 50 % de supercrítico). Cada nivel suma
 * (multiplicador − 1) al daño: con ×2, crítico = ×2 y supercrítico = ×3.
 */
export function rollDamage(base: number, critChance: number, critMultiplier: number, random: () => number): DamageRoll {
  const chance = Math.max(0, critChance);
  const guaranteed = Math.floor(chance);
  const critLevel = guaranteed + (random() < chance - guaranteed ? 1 : 0);
  return { amount: base * (1 + critLevel * (critMultiplier - 1)), critLevel };
}

/** Daño recibido tras la armadura: cada 100 de armadura reduce el daño a la mitad del restante. */
export function mitigate(damage: number, armor: number): number {
  return (damage * 100) / (100 + Math.max(0, armor));
}
