import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MerchantService } from '../../../core/services/merchant.service';

/**
 * Merchant color management: list colors (with a hex swatch), create/edit via an
 * inline form, and delete. Deleting a color still referenced by a product
 * variant surfaces the server 409 message.
 */
@Component({
  selector: 'app-merchant-colors',
  standalone: true,
  imports: [TranslatePipe,CommonModule, FormsModule],
  templateUrl: './merchant-colors.component.html',
  styleUrl: './merchant-colors.component.css'
})
export class MerchantColorsComponent implements OnInit {
  colors: any[] = [];
  loading = false;
  error: string | null = null;

  // Inline create/edit form model.
  editing: any = null;
  form: any = this.blankForm();

  constructor(private merchant: MerchantService) {}

  ngOnInit(): void {
    this.refresh();
  }

  blankForm(): any {
    return { name: '', hexCode: '#000000', displayOrder: 0, isActive: true };
  }

  refresh(): void {
    this.loading = true;
    this.merchant.getColors().subscribe({
      next: list => {
        this.colors = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load colors.';
        this.loading = false;
      }
    });
  }

  startCreate(): void {
    this.editing = null;
    this.form = this.blankForm();
  }

  startEdit(c: any): void {
    this.editing = c;
    this.form = {
      name: c.name,
      hexCode: c.hexCode ?? '#000000',
      displayOrder: c.displayOrder,
      isActive: c.isActive
    };
  }

  cancel(): void {
    this.editing = null;
    this.form = this.blankForm();
  }

  save(): void {
    this.error = null;

    if (this.editing) {
      this.merchant.updateColor(this.editing.id, this.form).subscribe({
        next: () => {
          this.cancel();
          this.refresh();
        },
        error: err => (this.error = err?.error?.message ?? 'Save failed.')
      });
      return;
    }

    this.merchant.createColor(this.form).subscribe({
      next: res => {
        if (res.success) {
          this.cancel();
          this.refresh();
        } else {
          this.error = res.message ?? 'Create failed.';
        }
      },
      error: err => (this.error = err?.error?.message ?? 'Create failed.')
    });
  }

  /** Deletes a color; surfaces the 409 message when it is still referenced. */
  remove(c: any): void {
    this.error = null;
    this.merchant.deleteColor(c.id).subscribe({
      next: () => this.refresh(),
      error: err => (this.error = err?.error?.message ?? 'Delete failed.')
    });
  }
}
