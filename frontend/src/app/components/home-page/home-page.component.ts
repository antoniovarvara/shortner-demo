import { Component, OnInit } from '@angular/core';
import { ShortUrlService } from 'src/app/services/short-url-service';
import { Validators, FormGroup, FormControl, ValidatorFn, AbstractControl } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-home-page',
  templateUrl: './home-page.component.html',
  styleUrls: ['./home-page.component.css']
})
export class HomePageComponent implements OnInit {

  urlControl: FormControl;
  createdUrl: string;
  generatingUrl: boolean;

  constructor(
    private shortUrlService: ShortUrlService,
    private snackBar: MatSnackBar) { }

  ngOnInit() {
    this.generatingUrl = false;
    let regex = '(http[s]?://)?(www\.)?([\\da-zA-Z0-9.-]+)\\.([a-z.]{2,6})[/\\w .-]*/?.*';
    //let regex = '^(http[s]?:\/\/){0,1}(www\.){0,1}[a-zA-Z0-9\.\-]+\.[a-zA-Z]{2,5}[\.]{0,1}'
    this.urlControl = new FormControl('', [
        Validators.required,
        Validators.pattern(regex),
      ]);
  }
 

  getErrorMessage() {
    if (this.urlControl.hasError('required')) {
      return 'You must enter a value';
    }

    return this.urlControl.hasError('url') ? 'Not a valid url' : 'Invalid url';
  }

  enableUrlGeneration(){
    return !this.generatingUrl && this.urlControl.status != "INVALID"
  }

  createNewUrl(){
    console.log(this.urlControl)
    if(!this.enableUrlGeneration()){
      return;
    }
    let urlToSend = this.urlControl.value
    if(!(urlToSend.startsWith("http://") || urlToSend.startsWith("https://"))){
      urlToSend = "https://" + urlToSend
    }
    this.generatingUrl = true;
    this.shortUrlService.createShortUrl(urlToSend).subscribe(
      (result) => {
        console.log(result);
        this.createdUrl = result.completeUrl;
        this.generatingUrl = false;
      },
      (err) =>{
        console.log(err);
        this.snackBar.open(err.message, undefined, {
          duration: 4000,
        });
        this.generatingUrl = false;
      })
  }

  redirect(){
    window.open(this.createdUrl);
  }

}

export function forbiddenNameValidator(nameRe: RegExp): ValidatorFn {
  return (control: AbstractControl): {[key: string]: any} | null => {
    const forbidden = nameRe.test(control.value);
    return forbidden ? {'forbiddenName': {value: control.value}} : null;
  };
}
