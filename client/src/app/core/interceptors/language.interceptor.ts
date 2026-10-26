import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { LanguageService } from '../i18n/language.service';
import { environment } from '../../../environments/environment';
export const languageInterceptor:HttpInterceptorFn=(req,next)=>next(req.url.startsWith(environment.apiBaseUrl+'/')?req.clone({setHeaders:{'Accept-Language':inject(LanguageService).language}}):req);
