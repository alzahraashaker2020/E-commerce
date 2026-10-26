import { DatePipe, DecimalPipe, registerLocaleData } from '@angular/common';
import localeAr from '@angular/common/locales/ar';
import { Pipe, PipeTransform } from '@angular/core';
import { LanguageService } from './language.service';
registerLocaleData(localeAr,'ar-EG');
@Pipe({name:'localDate',standalone:true,pure:false})
export class LocalDatePipe implements PipeTransform{
 constructor(private language:LanguageService){}
 transform(value:Date|string|number|null|undefined,format='mediumDate',timezone?:string,locale?:string):string|null{return new DatePipe(locale||this.language.locale).transform(value,format,timezone);}
}
@Pipe({name:'localNumber',standalone:true,pure:false})
export class LocalNumberPipe implements PipeTransform{
 constructor(private language:LanguageService){}
 transform(value:string|number|null|undefined,digits?:string):string|null{return new DecimalPipe(this.language.locale).transform(value,digits);}
}
