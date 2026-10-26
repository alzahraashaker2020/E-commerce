import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { Cart } from '../../core/models/api.models';
import { StoreCurrencyPipe } from '../../core/pipes/store-currency.pipe';

/**
 * Cart page. Loads the raw cart lines from CartService and asks the backend to
 * re-price them (authoritative server-side prices). Quantity edits and removals
 * update the local cart and re-validate.
 */
@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [TranslatePipe,CommonModule, RouterLink, StoreCurrencyPipe],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.css'
})
export class CartComponent implements OnInit {
  cart: Cart | null = null;
  loading = false;
  error: string | null = null;

  constructor(private cartService: CartService, private router: Router) {}

  ngOnInit(): void {
    this.refresh();
  }

  /** Whether the local cart has no lines. */
  get isEmpty(): boolean {
    return this.cartService.isEmpty();
  }

  /** Re-prices the current cart against the backend. */
  refresh(): void {
    if (this.cartService.isEmpty()) {
      this.cart = { lines: [], subtotal: 0 };
      return;
    }
    this.loading = true;
    this.error = null;
    this.cartService.validate().subscribe({
      next: c => {
        this.cart = c;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not price the cart. Is the API running?';
        this.loading = false;
      }
    });
  }

  changeQty(productId: number, variantId: number | null | undefined, qty: number): void {
    this.cartService.updateQuantity(productId, variantId, qty);
    this.refresh();
  }

  remove(productId: number, variantId: number | null | undefined): void {
    this.cartService.remove(productId, variantId);
    this.refresh();
  }

  checkout(): void {
    this.router.navigate(['/checkout']);
  }
}
