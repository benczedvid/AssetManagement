import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar/navbar';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    NavbarComponent,
    RouterOutlet
  ],
  templateUrl: './app-layout.html'
})
export class AppLayoutComponent {

}
