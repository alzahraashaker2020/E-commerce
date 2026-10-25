import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { CheckoutService } from '../../core/services/checkout.service';
import { Cart, PlaceOrderRequest } from '../../core/models/api.models';
import { StoreCurrencyPipe } from '../../core/pipes/store-currency.pipe';

/**
 * Guest checkout page. Collects the guest's name/email and an optional discount
 * code, shows the server-priced cart, and places the order. No account is
 * required — the order confirmation carries the tracking number.
 */
@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, StoreCurrencyPipe],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css'
})
export class CheckoutComponent implements OnInit {
  cart: Cart | null = null;
  guestName = '';
  guestEmail = '';
  discountCode = '';
  addressLine1 = '';
  addressLine2 = '';
  city = '';
  stateRegion = '';
  postalCode = '';
  country = '';
  contactPhone = '';
  paymentMethod: 'Cash' | 'Card' = 'Cash';
  submitting = false;
  submitted = false;
  error: string | null = null;

  constructor(
    private cartService: CartService,
    private checkout: CheckoutService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Price the cart for display; redirect to the cart page when empty.
    if (this.cartService.isEmpty()) {
      this.router.navigate(['/cart']);
      return;
    }
    this.cartService.validate().subscribe({
      next: c => (this.cart = c),
      error: () => (this.error = 'Could not price the cart. Is the API running?')
    });
  }

  get canSubmit(): boolean {
    return (
      this.guestName.trim().length > 0 &&
      this.guestEmail.trim().length > 3 &&
      this.addressLine1.trim().length > 0 &&
      this.city.trim().length > 0 &&
      this.stateRegion.trim().length > 0 &&
      this.postalCode.trim().length > 0 &&
      this.country.trim().length > 0 &&
      this.contactPhone.trim().length > 0 &&
      (this.paymentMethod === 'Cash' || this.paymentMethod === 'Card') &&
      !this.submitting
    );
  }

  /** Labels of the required fields that are still empty, for inline validation. */
  get missingFields(): string[] {
    const missing: string[] = [];
    if (this.guestName.trim().length === 0) missing.push('Full name');
    if (this.guestEmail.trim().length === 0) missing.push('Email');
    if (this.addressLine1.trim().length === 0) missing.push('Address line 1');
    if (this.city.trim().length === 0) missing.push('City');
    if (this.stateRegion.trim().length === 0) missing.push('State/region');
    if (this.postalCode.trim().length === 0) missing.push('Postal code');
    if (this.country.trim().length === 0) missing.push('Country');
    if (this.contactPhone.trim().length === 0) missing.push('Contact phone');
    if (this.paymentMethod !== 'Cash' && this.paymentMethod !== 'Card') missing.push('Payment method');
    return missing;
  }

  /** Places the guest order and navigates to the confirmation page. */
  placeOrder(): void {
    this.submitted = true;
    if (!this.canSubmit) {
      return;
    }
    this.submitting = true;
    this.error = null;

    const request: PlaceOrderRequest = {
      guestName: this.guestName.trim(),
      guestEmail: this.guestEmail.trim(),
      discountCode: this.discountCode.trim() || null,
      lines: this.cartService.getLines(),
      addressLine1: this.addressLine1.trim(),
      addressLine2: this.addressLine2.trim() || null,
      city: this.city.trim(),
      stateRegion: this.stateRegion.trim(),
      postalCode: this.postalCode.trim(),
      country: this.country.trim(),
      contactPhone: this.contactPhone.trim(),
      paymentMethod: this.paymentMethod
    };

    this.checkout.placeOrder(request).subscribe({
      next: res => {
        this.submitting = false;
        if (res.data?.orderNumber) {
          // Clear the cart and go to the confirmation page; pass the full
          // confirmation payload so that page can show address/payment/currency.
          this.cartService.clear();
          this.router.navigate(['/confirmation', res.data.orderNumber], {
            state: { message: res.message, confirmation: res.data }
          });
        } else {
          this.error = res.message ?? 'Order could not be placed.';
        }
      },
      error: (err) => {
        this.submitting = false;
        this.error = err?.error?.message ?? 'Order could not be placed. Please try again.';
      }
    });
  }
}
