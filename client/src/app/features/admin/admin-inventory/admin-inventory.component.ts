import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';

/**
 * Admin inventory alerts: lists every inventory row currently at or below its
 * low-stock threshold so the owner can reorder.
 */
@Component({
  selector: 'app-admin-inventory',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-inventory.component.html',
  styleUrl: './admin-inventory.component.css'
})
export class AdminInventoryComponent implements OnInit {
  items: any[] = [];
  loading = false;
  error: string | null = null;

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
    this.loading = true;
    this.admin.getLowStock().subscribe({
      next: list => {
        this.items = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load inventory alerts.';
        this.loading = false;
      }
    });
  }
}
