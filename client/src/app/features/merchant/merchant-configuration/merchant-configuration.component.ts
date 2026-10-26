import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { SettingsService } from '../../../core/services/settings.service';

@Component({
  standalone: true,
  imports: [TranslatePipe,CommonModule, FormsModule],
  templateUrl: './merchant-configuration.component.html',
  styles: ['.form-grid { display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:1rem } .card{margin-bottom:1rem} .actions{display:flex;gap:.5rem;flex-wrap:wrap} .table-wrap{overflow-x:auto}']
})
export class MerchantConfigurationComponent implements OnInit {
  currencies = false;
  rows: any[] = [];
  behaviors: string[] = [];
  form: any = {};
  editing: number | null = null;
  error = '';
  notice = '';
  busy = false;
  storeCode = '';
  constructor(private route: ActivatedRoute, private api: ApiService, private settings: SettingsService) {}
  get path(): string { return this.currencies ? 'merchant/currencies' : 'merchant/order-statuses'; }
  ngOnInit(): void {
    this.currencies = this.route.snapshot.data['kind'] === 'currencies';
    this.reset(); this.load();
    if (!this.currencies) this.api.get<string[]>('merchant/order-statuses/behaviors').subscribe({ next: b => this.behaviors = b, error: e => this.fail(e) });
    else this.api.get<any>('merchant/settings').subscribe({ next: s => this.storeCode = s.currencyCode, error: e => this.fail(e) });
  }
  load(): void { this.api.get<any[]>(this.path).subscribe({ next: rows => this.rows = rows, error: e => this.fail(e) }); }
  reset(): void { this.editing = null; this.form = { name: '', code: '', symbol: '', color: '#8a776e', behavior: 'Pending', displayOrder: 0, isActive: true }; }
  edit(row: any): void { if (this.busy) return; this.editing = row.id; this.form = { ...row }; this.error = ''; this.notice = ''; }
  save(): void {
    if (this.busy) return;
    this.error = ''; this.notice = ''; this.busy = true;
    const request = this.editing ? this.api.put(this.path + '/' + this.editing, this.form) : this.api.post(this.path, this.form);
    request.subscribe({ next: () => { this.busy = false; this.reset(); this.load(); this.notice = 'Saved successfully.'; if (this.currencies) this.settings.load().subscribe(); }, error: e => this.fail(e) });
  }
  remove(row: any): void {
    if (this.busy) return;
    this.busy = true; this.error = ''; this.notice = '';
    this.api.delete(this.path + '/' + row.id).subscribe({ next: () => { this.busy = false; this.load(); if (this.editing === row.id) this.reset(); }, error: e => this.fail(e) });
  }
  setStoreCurrency(): void {
    const currency = this.rows.find(r => r.code === this.storeCode && r.isActive);
    if (!currency || this.busy) return;
    this.busy = true; this.error = ''; this.notice = '';
    this.api.put('merchant/settings', { currencyCode: currency.code, currencySymbol: currency.symbol }).subscribe({
      next: () => { this.busy = false; this.notice = 'Store currency updated. Existing orders keep their original currency.'; this.settings.load().subscribe(); }, error: e => this.fail(e)
    });
  }
  private fail(error: any): void { this.busy = false; this.error = error?.error?.message || 'Could not save or load this configuration.'; }
}
