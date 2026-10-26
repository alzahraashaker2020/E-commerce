import { LanguageService } from './core/i18n/language.service';
import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { RuntimeService } from './core/services/runtime.service';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { languageInterceptor } from './core/interceptors/language.interceptor';
import { authInterceptor } from './core/interceptors/auth.interceptor';

/**
 * Root application providers:
 * - Router with in-memory scrolling enabled (the catalog additionally manages
 *   explicit page/scroll restoration for preserved browse position).
 * - HttpClient with the functional auth interceptor that attaches the merchant JWT.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    {provide:APP_INITIALIZER,multi:true,deps:[LanguageService],useFactory:(language:LanguageService)=>()=>language.initialize()},
    {provide:APP_INITIALIZER,multi:true,deps:[RuntimeService],useFactory:(runtime:RuntimeService)=>()=>runtime.initialize()},
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      routes,
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' })
    ),
    provideHttpClient(withInterceptors([authInterceptor,languageInterceptor]))
  ]
};
