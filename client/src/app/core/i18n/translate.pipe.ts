import { Pipe, PipeTransform } from '@angular/core';
import { LanguageService } from './language.service';
@Pipe({name:'t',standalone:true,pure:false})
export class TranslatePipe implements PipeTransform {
  constructor(private language:LanguageService){}
  transform<T>(value:T):T|string{return this.language.text(value);}
}
