import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../../environments/environment';

/** Resolves API uploads separately from storefront assets. */
@Pipe({ name: 'imageUrl', standalone: true })
export class ImageUrlPipe implements PipeTransform {
  transform(path: string | null | undefined): string {
    if (!path) return '';
    if (!path.startsWith('/uploads/')) return path;
    const apiOrigin = environment.apiBaseUrl.replace(/\/api\/?$/, '');
    return apiOrigin + path;
  }
}
