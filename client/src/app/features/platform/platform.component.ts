import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { RouterLink } from '@angular/router';

interface StoreRow { id: string; name: string; host: string; state: string; }

@Component({
  standalone: true,
  imports: [TranslatePipe,CommonModule, FormsModule, RouterLink],
  templateUrl:'./platform.component.html',
  styleUrl:'./platform.component.css'

})
export class PlatformComponent {
  signedIn = !!localStorage.getItem('shop_platform_token');
  username = ''; password = ''; error = ''; busy = false;
  stores: StoreRow[] = [];
  get activeStores():number{return this.stores.filter(s=>s.state==='Active').length;}
  get pausedStores():number{return this.stores.filter(s=>s.state==='Suspended').length;}
  draft = { name: '', host: '', connectionKey: '', ownerUsername: '', ownerEmail: '', ownerPassword: '' };
  constructor(private api: ApiService) { if (this.signedIn) this.load(); }
  login(): void {
    this.busy = true; this.error = '';
    this.api.post<{token: string}>('platform/auth/login', { username: this.username, password: this.password }).subscribe({
      next: result => { localStorage.setItem('shop_platform_token', result.token); this.password = ''; this.signedIn = true; this.busy = false; this.load(); },
      error: () => { this.error = 'Platform sign in failed.'; this.busy = false; }
    });
  }
  logout(): void { localStorage.removeItem('shop_platform_token'); this.signedIn = false; this.stores = []; }
  load(): void { this.api.get<StoreRow[]>('platform/stores').subscribe({ next: stores => this.stores = stores, error: err => { this.error = err.error?.message || 'Unable to load stores.'; if (err.status === 401) this.logout(); } }); }
  create(): void {
    this.busy = true; this.error = '';
    this.api.post('platform/stores', this.draft).subscribe({ next: () => { this.busy = false; this.draft.ownerPassword = ''; this.load(); }, error: err => { this.busy = false; this.error = err.error?.message || 'Unable to create store.'; } });
  }
  changeState(store: StoreRow): void {
    this.busy = true; this.error = '';
    this.api.put('platform/stores/' + store.id + '/state', { state: store.state === 'Active' ? 'Suspended' : 'Active' }).subscribe({ next: () => { this.busy = false; this.load(); }, error: err => { this.busy = false; this.error = err.error?.message || 'Unable to update store.'; } });
  }
}
