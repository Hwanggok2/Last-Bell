(() => {
  const value = (id, min, max) => {
    const input=document.getElementById(id);
    const number=Number(input.value);
    return Math.max(min,Math.min(max,Number.isFinite(number)?number:0));
  };
  const won=number=>`${Math.round(number).toLocaleString('ko-KR')}원`;
  const render=()=>{
    const mau=value('calc-mau',0,1000000),days=value('calc-days',0,30);
    const payer=value('calc-payer',0,100)/100,arppu=value('calc-arppu',0,8800);
    const impressions=value('calc-ads',0,3),ecpm=value('calc-ecpm',0,1000000);
    const fee=value('calc-fee',0,1),cost=value('calc-cost',0,1000000000);
    const dau=mau*days/30;
    const total=mau*payer*arppu*.98/1.1*(1-fee)+dau*30*impressions*ecpm/1000;
    document.getElementById('calc-dau').textContent=Math.round(dau).toLocaleString('ko-KR');
    document.getElementById('calc-total').textContent=won(total);
    document.getElementById('calc-net').textContent=won(total-cost);
  };
  document.querySelectorAll('.calc input,.calc select').forEach(input=>input.addEventListener('input',render));
  render();
})();
