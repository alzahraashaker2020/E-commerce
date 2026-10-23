import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Cart, CartLineRequest } from '../models/api.models';

/**
 * Client-side cart. The cart contents (product/variant/quantity) live in
 * localStorage so they survive a page refresh; prices are NEVER trusted from
 * the client — the backend CartController re-prices every line server-side via
 * `validate()`, and checkout re-prices again. A reactive line count is exposed
 * for the header badge.
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private static readonly STORAGE_KEY = 'shop_demo_cart';

  private lines: CartLineRequest[] = this.load();
  private readonly countSubject = new BehaviorSubject<number>(this.totalQuantity());

  /** Observable total quantity for the header cart badge. */
  readonly count$: Observable<number> = this.countSubject.asObservable();

  constructor(private api: ApiService) {}

  /** Returns a copy of the current raw cart lines. */
  getLines(): CartLineRequest[] {
    return this.lines.map(l => ({ ...l }));
  }

  /** Adds a line (or increments the quantity of a matching product+variant). */
  add(productId: number, quantity: number, variantId?: number | null): void {
    const existing = this.lines.find(l => l.productId === productId && (l.variantId ?? null) === (variantId ?? null));
    if (existing) {
      existing.quantity = Math.min(100, existing.quantity + quantity);
    } else {
      this.lines.push({ productId, variantId: variantId ?? null, quantity: Math.min(100, quantity) });
    }
    this.persist();
  }

  /** Updates a line quantity; removes the line when quantity reaches 0. */
  updateQuantity(productId: number, variantId: number | null | undefined, quantity: number): void {
    const line = this.lines.find(l => l.productId === productId && (l.variantId ?? null) === (variantId ?? null));
    if (!line) {
      return;
    }
    if (quantity <= 0) {
      this.remove(productId, variantId);
      return;
    }
    line.quantity = Math.min(100, quantity);
    this.persist();
  }

  /** Removes a line entirely. */
  remove(productId: number, variantId?: number | null): void {
    this.lines = this.lines.filter(l => !(l.productId === productId && (l.variantId ?? null) === (variantId ?? null)));
    this.persist();
  }

  /** Empties the cart (used after a successful checkout). */
  clear(): void {
    this.lines = [];
    this.persist();
  }

  /** Whether the cart has any lines. */
  isEmpty(): boolean {
    return this.lines.length === 0;
  }

  /**
   * Asks the backend to validate and re-price the current cart. The returned
   * Cart carries authoritative server-side prices and the subtotal.
   */
  validate(): Observable<Cart> {
    return this.api.post<Cart>('cart/validate', this.lines);
  }

  // --- internal helpers ---------------------------------------------------

  private totalQuantity(): number {
    return this.lines.reduce((sum, l) => sum + l.quantity, 0);
  }

  private persist(): void {
    localStorage.setItem(CartService.STORAGE_KEY, JSON.stringify(this.lines));
    this.countSubject.next(this.totalQuantity());
  }

  private load(): CartLineRequest[] {
    try {
      const raw = localStorage.getItem(CartService.STORAGE_KEY);
      return raw ? (JSON.parse(raw) as CartLineRequest[]) : [];
    } catch {
      // Corrupt/unavailable storage: start with an empty cart.
      return [];
    }
  }
}
