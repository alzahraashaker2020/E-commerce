import { LanguageService } from '../i18n/language.service';
import '../i18n/format.pipe';
import { CurrencyPipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';
import { SettingsService } from '../services/settings.service';

/**
 * Formats a number as a price using the store-configured currency
 * (GET api/settings via SettingsService), falling back to USD. Replaces the
 * built-in `| currency` pipe across the storefront and merchant templates so a
 * single currency setting drives every displayed price.
 */
@Pipe({
  name: 'storeCurrency',
  standalone: true,
  pure: false
})
export class StoreCurrencyPipe implements PipeTransform {

  /**
   * SettingsService is injected in the constructor (never as a field
   * initializer — TS2729) so the service is available before use.
   */
  constructor(private settings: SettingsService,private language:LanguageService) {}

  /**
   * Formats the supplied value with the store currency code.
   * @param value The numeric amount (or null/undefined).
   * @param currencyCode Optional recorded order currency; otherwise the current store currency.
   * @param digits Optional digits info passed through to CurrencyPipe.
   */
  transform(
    value: number | null | undefined,
    currencyCode?: string | null,
    digits?: string
  ): string | null {
    const code = currencyCode || this.settings.currencyCode() || 'USD';
    const display = code === this.settings.currencyCode() ? this.settings.currencySymbol() : 'symbol';
    return new CurrencyPipe(this.language.locale).transform(value, code, String(this.language.text(display)), digits);
  }
}
