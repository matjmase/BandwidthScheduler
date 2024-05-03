import {
  Directive,
  ElementRef,
  Input,
  OnChanges,
  OnInit,
  SimpleChanges,
} from '@angular/core';
import { IColorModel } from '../schedule-publisher/ColoredTimeFrameModel';

@Directive({
  selector: '[appColorElement]',
})
export class ColorElementDirective implements OnInit, OnChanges {
  @Input() appColorElement: IColorModel = { R: 255, G: 255, B: 255 };

  constructor(private element: ElementRef) {}

  ngOnInit(): void {
    this.UpdateElementBackground();
  }
  ngOnChanges(changes: SimpleChanges): void {
    this.UpdateElementBackground();
  }

  private UpdateElementBackground() {
    this.element.nativeElement.style.backgroundColor = `rgb(${this.appColorElement.R}, ${this.appColorElement.G}, ${this.appColorElement.B})`;
  }
}
