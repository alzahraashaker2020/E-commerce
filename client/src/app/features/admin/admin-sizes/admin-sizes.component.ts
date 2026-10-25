import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';

/**
 * Admin size management: list sizes, create/edit via an inline form, and delete.
 * Deleting a size still referenced by a product variant surfaces the server 409
 * message.
 */
@Component({
  selector: 'app-admin-sizes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-sizes.component.html',
  styleUrl: './admin-sizes.component.css'
})
export class AdminSizesComponent implements OnInit {
  sizes: any[] = [];
  loading = false;
  error: string | null = null;

  // Inline create/edit form model.
  editing: any = null;
  form: any = this.blankForm();

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
    this.refresh();
  }

  blankForm(): any {
    return { name: '', displayOrder: 0, isActive: true };
  }

  refresh(): void {
    this.loading = true;
    this.admin.getSizes().subscribe({
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
      this.admin.updateSize(this.editing.id, this.form).subscribe({
        next: () => {
          this.cancel();
          this.refresh();
        },
        error: err => (this.error = err?.error?.message ?? 'Save failed.')
      });
      return;
    }

    this.admin.createSize(this.form).subscribe({
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
    this.admin.deleteSize(s.id).subscribe({
      next: () => this.refresh(),
      error: err => (this.error = err?.error?.message ?? 'Delete failed.')
    });
  }
}
