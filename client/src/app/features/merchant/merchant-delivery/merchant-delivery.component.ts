import { LocalNumberPipe } from '../../../core/i18n/format.pipe';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({ standalone: true, imports: [LocalNumberPipe,TranslatePipe,CommonModule, FormsModule], templateUrl: './merchant-delivery.component.html',
  styles: ['.card{margin-bottom:1rem}.form-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:1rem}.actions{display:flex;gap:.5rem;flex-wrap:wrap}.table-wrap{overflow-x:auto}'] })
export class MerchantDeliveryComponent implements OnInit {
  governorates: any[] = []; areas: any[] = []; currencies: any[] = [];
  governorate: any = { name: '', isActive: true }; area: any = {};
  error = ''; notice = ''; busy = false; storeCode = '';
  constructor(private api: ApiService) {}
  ngOnInit(): void {
    this.load(); this.resetArea();
    this.api.get<any[]>('merchant/currencies').subscribe({ next: c => this.currencies = c.filter(r => r.isActive), error: e => this.fail(e) });
    this.api.get<any>('merchant/settings').subscribe({ next: s => { this.storeCode = s.currencyCode; if (!this.area.id) this.area.currencyCode = s.currencyCode; }, error: e => this.fail(e) });
  }
  load(): void {
    this.api.get<any[]>('merchant/delivery/governorates').subscribe({ next: g => this.governorates = g, error: e => this.fail(e) });
    this.api.get<any[]>('merchant/delivery/areas').subscribe({ next: a => this.areas = a, error: e => this.fail(e) });
  }
  resetArea(): void { this.area = { name: '', governorateId: null, shippingFee: 0, currencyCode: this.storeCode, isActive: true }; }
  save(kind: 'governorates' | 'areas'): void {
    if (this.busy) return;
    const model = kind === 'areas' ? this.area : this.governorate;
    const path = 'merchant/delivery/' + kind;
    this.busy = true; this.error = ''; this.notice = '';
    const op = model.id ? this.api.put(path + '/' + model.id, model) : this.api.post(path, model);
    op.subscribe({ next: () => { this.busy = false; if (kind === 'areas') this.resetArea(); else this.governorate = { name: '', isActive: true }; this.notice = 'Saved successfully.'; this.load(); }, error: e => this.fail(e) });
  }
  remove(kind: string, id: number): void {
    if (this.busy) return;
    this.busy = true; this.error = ''; this.notice = '';
    this.api.delete('merchant/delivery/' + kind + '/' + id).subscribe({ next: () => { this.busy = false; this.load(); }, error: e => this.fail(e) });
  }
  editGovernorate(row: any): void { if (!this.busy) this.governorate = { ...row }; }
  editArea(row: any): void { if (!this.busy) this.area = { ...row }; }
  fail(error: any): void { this.busy = false; this.error = error?.error?.message || 'Could not complete delivery management.'; }
}
