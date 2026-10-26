import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MerchantService } from '../../../core/services/merchant.service';

/**
 * Merchant size management: list sizes, create/edit via an inline form, and delete.
 * Deleting a size still referenced by a product variant surfaces the server 409
 * message.
 */
@Component({
  selector: 'app-merchant-sizes',
  standalone: true,
  imports: [TranslatePipe,CommonModule, FormsModule],
  templateUrl: './merchant-sizes.component.html',
  styleUrl: './merchant-sizes.component.css'
})
export class MerchantSizesComponent implements OnInit {
  sizes: any[] = [];
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
    return { name: '', displayOrder: 0, isActive: true };
  }

  refresh(): void {
    this.loading = true;
    this.merchant.getSizes().subscribe({
      next: list => {
        this.sizes = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load sizes.';
        this.loading = false;
      }
    });
  }

  startCreate(): void {
    this.editing = null;
    this.form = this.blankForm();
  }

  startEdit(s: any): void {
    this.editing = s;
    this.form = { name: s.name, displayOrder: s.displayOrder, isActive: s.isActive };
  }

  cancel(): void {
    this.editing = null;
    this.form = this.blankForm();
  }

  save(): void {
    this.error = null;

    if (this.editing) {
      this.merchant.updateSize(this.editing.id, this.form).subscribe({
        next: () => {
          this.cancel();
          this.refresh();
        },
        error: err => (this.error = err?.error?.message ?? 'Save failed.')
      });
      return;
    }

    this.merchant.createSize(this.form).subscribe({
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

  /** Deletes a size; surfaces the 409 message when it is still referenced. */
  remove(s: any): void {
    this.error = null;
    this.merchant.deleteSize(s.id).subscribe({
      next: () => this.refresh(),
      error: err => (this.error = err?.error?.message ?? 'Delete failed.')
    });
  }
}
