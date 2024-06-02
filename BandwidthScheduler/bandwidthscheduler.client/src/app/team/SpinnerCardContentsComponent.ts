import { SpinnerCardHorizontalStretch } from '../commonControls/spinner-card/spinner-card.component';

export abstract class SpinnerCardContentsComponent {
  public WaitingOnSubmit: boolean = false;

  public abstract GetHorizontalStretch(): SpinnerCardHorizontalStretch;
}
