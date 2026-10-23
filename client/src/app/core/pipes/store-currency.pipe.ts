import { CurrencyPipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';
import { SettingsService } from '../services/settings.service';

/**
 * Formats a number as a price using the store-configured currency
 * (GET api/settings via SettingsService), falling back to USD. Replaces the
 * built-in `| currency` pipe across the storefront and admin templates so a
 * single currency setting drives every displayed price.
 */
@Pipe({
  name: 'storeCurrency',
  standalone: true,
  pure: true
})
export class StoreCurrencyPipe implements PipeTransform {
  private readonly currencyPipe = new CurrencyPipe('en-US');

  /**
   * SettingsService is injected in the constructor (never as a field
   * initializer — TS2729) so the service is available before use.
   */
  constructor(private settings: SettingsService) {}

  /**
   * Formats the supplied value with the store currency code.
   * @param value The numeric amount (or null/undefined).
   * @param display Symbol display mode passed through to CurrencyPipe.
   * @param digits Optional digits info passed through to CurrencyPipe.
   */
  transform(
    value: number | null | undefined,
    display: string | boolean = 'symbol',
    digits?: string
  ): string | null {
    const code = this.settings.currencyCode() || 'USD';
    return this.currencyPipe.transform(value, code, display, digits);
  }
}
